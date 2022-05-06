using System;
using BinaryEx;

namespace Fleck.Handlers
{
    internal ref struct FrameReadResult
    {
        public bool FrameFullyParsed;
        public bool FrameHeaderFullyParsed;
        public bool EndOfMessage;
        public byte ReservedBits;
        public FrameType FrameType;
        public bool IsMasked;
        public int PayloadLength;
        public int DataReadEndPos;
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
            int pos = 0;

            byte frameOpcode = (byte)frameType;
            byte frameFinal = (byte)(endOfMessage ? 0x80 : 0x00); // Fragmented packets 

            byte maskedFrameData = (byte)(maskedFrame ? 0x00 : 0x80); // this is a masked frame 

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

            // TODO - implement mask key writing for client -> server sending support

            if (payload.Length > 0)
            {
                dataOut.WriteBytes(ref pos, payload);
            }
            return pos;
        }

        /// <summary>
        /// Read websocket frame data
        /// </summary>
        /// <param name="data">Raw incoming data from socket</param>
        /// <param name="messageDataOut">message data</param>
        /// <returns>number of bytes read from data stream, if 0 no data was read and messageDataOut is not valid data</returns>
        internal static FrameReadResult ReadFrame(ReadOnlySpan<byte> data, Span<byte> messageDataOut)
        {
            var dataLen = data.Length;
            FrameReadResult result = new FrameReadResult();

            if (dataLen < 2)
                return result;

            result.EndOfMessage = (data[0] & 128) != 0;
            result.ReservedBits = (byte)((data[0] & 112) >> 4);
            result.FrameType = (FrameType)(data[0] & 15);
            result.IsMasked = (data[1] & 128) != 0;
            var length = data[1] & 127;

            if (!result.IsMasked || !result.FrameType.IsDefined() || result.ReservedBits != 0)
                throw new WebSocketException(WebSocketStatusCodes.ProtocolError);

            int index = 2;

            if (length == 127)
            {
                if (dataLen < index + 8)
                    return result;

                long longLength = data.ReadInt64BE(ref index);

                // In C# we cannot represent any byte arrays longer than an int32 directly
                // So we need to close the connection if this occurs
                if (longLength > (Int32.MaxValue - index + 4))
                {
                    throw new WebSocketException(WebSocketStatusCodes.MessageTooBig);
                }
                result.PayloadLength = (int)longLength;
            }
            else if (length == 126)
            {
                if (dataLen < index + 2)
                    return result;
                result.PayloadLength = data.ReadUInt16BE(ref index);
            }
            else
            {
                result.PayloadLength = length;
            }
            if (dataLen < index + 4)
                return result;

            var maskBytes = data.Slice(index, 4);
            index += 4;
            result.FrameHeaderFullyParsed = true;

            var bytesUsed = index + result.PayloadLength;

            if (dataLen < bytesUsed)
                return result;


            var payloadData = data.Slice(index, result.PayloadLength);
            for (var i = 0; i < result.PayloadLength; i++)
            {
                messageDataOut[i] = (byte)(payloadData[i] ^ maskBytes[i % 4]);
            }
            result.DataReadEndPos = bytesUsed;
            result.FrameFullyParsed = true;

            // TODO need some way to return additional frame data to caller
            return result;
        }
    }
}
