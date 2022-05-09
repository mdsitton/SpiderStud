using System;
using System.Security.Cryptography;
using BinaryEx;

namespace SpiderStud
{
    internal ref struct FrameReadState
    {
        public bool FrameHeaderFullyParsed;
        public bool PayloadReady;
        public bool EndOfMessage;
        public byte ReservedBits;
        public FrameType FrameType;
        public bool IsMasked;
        public ReadOnlySpan<byte> MaskKey;
        public int PayloadLength;
        public int PayloadStartOffset;
        public WebSocketStatusCode ErrorCode;
    }

    internal class FrameParsing
    {
        /// <summary>
        /// Calculate max frame size based on input payload
        /// </summary>
        /// <param name="payload"></param>
        /// <returns>max frame size</returns>
        internal static int GetMaxFrameSize(ReadOnlySpan<byte> payload) => payload.Length + 14;

        internal static int WriteFrame(Span<byte> dataOut, ReadOnlySpan<byte> payload, FrameType frameType, bool endOfMessage = true, bool maskedFrame = false)
        {
            // Only allocate stack bytes if we are writing out a masked frame
            Span<byte> maskKey = maskedFrame ? stackalloc byte[4] : Span<byte>.Empty;

            if (maskedFrame)
            {
                RandomNumberGenerator.Fill(maskKey);
            }
            return WriteFrame(dataOut, payload, frameType, maskKey, endOfMessage, maskedFrame);
        }

        internal static int WriteFrame(Span<byte> dataOut, ReadOnlySpan<byte> payload, FrameType frameType, Span<byte> maskKey, bool endOfMessage = true, bool maskedFrame = false)
        {
            int pos = 0;

            byte frameOpcode = (byte)frameType;
            byte frameFinal = (byte)(endOfMessage ? 0x80 : 0x00); // Fragmented packets 

            byte maskedFrameData = (byte)(maskedFrame ? 0x80 : 0x00); // this is a masked frame 

            byte byte1 = (byte)(frameFinal | frameOpcode);

            dataOut.WriteByte(ref pos, byte1);

            byte byte2 = maskedFrameData;

            // writer.
            if (payload.Length > ushort.MaxValue)
            {
                byte2 |= 127;
                dataOut.WriteByte(ref pos, byte2);
                dataOut.WriteUInt64BE(ref pos, (ulong)payload.Length);
            }
            else if (payload.Length > 125)
            {
                byte2 |= 126;
                dataOut.WriteByte(ref pos, byte2);
                dataOut.WriteUInt16BE(ref pos, (ushort)payload.Length);
            }
            else
            {
                byte2 |= (byte)payload.Length;
                dataOut.WriteByte(ref pos, byte2);
            }

            if (maskedFrame)
            {
                dataOut.WriteBytes(ref pos, maskKey);
            }

            int payloadStart = pos;

            if (payload.Length > 0)
            {
                dataOut.WriteBytes(ref pos, payload);
            }

            if (maskedFrame)
            {
                // mask payload data in-place
                Span<byte> payloadLocation = dataOut.Slice(payloadStart, payload.Length);
                ApplyDataMasking(payloadLocation, payloadLocation, maskKey);
            }

            return pos;
        }

        internal static void ApplyDataMasking(ReadOnlySpan<byte> dataIn, Span<byte> dataOut, ReadOnlySpan<byte> maskKey)
        {
            for (var i = 0; i < dataIn.Length; i++)
            {
                // i & (4-1) is equivelent to i % 4 this is just an optimization
                dataOut[i] = (byte)(dataIn[i] ^ maskKey[i & 3]);
            }
        }

        /// <summary>
        /// Parse websocket frame header while validating if more data should arrive
        /// </summary>
        /// <param name="dataIn">Raw incoming data from socket</param>
        /// <returns>
        /// Returns <see cref="FrameReadState"/> with the parser state.
        /// </returns>
        internal static FrameReadState ReadFrameHeader(ReadOnlySpan<byte> dataIn)
        {
            var dataLen = dataIn.Length;
            FrameReadState result = new FrameReadState();


            // verify we have first two bytes of frame data
            if (dataLen < 2)
                return result;

            result.EndOfMessage = (dataIn[0] & 0x80) != 0;
            result.ReservedBits = (byte)((dataIn[0] & 0x70) >> 4);
            result.FrameType = (FrameType)(dataIn[0] & 0xf);
            result.IsMasked = (dataIn[1] & 0x80) != 0;
            var length = dataIn[1] & 0x7f;

            if (!result.FrameType.IsDefined() || result.ReservedBits != 0)
            {
                result.ErrorCode = WebSocketStatusCode.ProtocolError;
                return result;
            }

            int index = 2;

            if (length == 127) // int64 payload length
            {
                // verify we have the data required for extended length
                if (dataLen < index + 8)
                    return result;

                long longLength = dataIn.ReadInt64BE(ref index);

                // In C# we cannot represent any byte arrays longer than an int32 directly
                // So we need to close the connection if this occurs
                if (longLength > (Int32.MaxValue - index + 4))
                {
                    result.ErrorCode = WebSocketStatusCode.MessageTooBig;
                    return result;
                }
                result.PayloadLength = (int)longLength;
            }
            else if (length == 126) // uint16 payload length
            {
                // verify we have the data required for extended length
                if (dataLen < index + 2)
                    return result;
                result.PayloadLength = dataIn.ReadUInt16BE(ref index);
            }
            else  // byte payload length 0-125
            {
                result.PayloadLength = length;
            }

            if (result.IsMasked)
            {
                // verify we have key data available
                if (dataLen < index + 4)
                    return result;

                result.MaskKey = dataIn.Slice(index, 4);
                index += 4;
            }
            result.FrameHeaderFullyParsed = true;
            result.PayloadStartOffset = index;

            // if we have the full data payload available this will be set to true
            // If not the caller will need to know to wait on the remaining payload data to arrive
            result.PayloadReady = dataLen >= (index + result.PayloadLength);

            return result;
        }
    }
}
