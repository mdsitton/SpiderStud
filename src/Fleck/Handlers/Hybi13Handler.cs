using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Fleck.Handlers
{
    internal class Hybi13Handler : IHandler
    {
        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly Encoding UTF8 = new UTF8Encoding(false, true);
        private static readonly SHA1 SHA1 = SHA1.Create();
        private static readonly ThreadLocal<StringBuilder> StringBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(1024));

        private readonly ReadState _readState;
        private readonly WebSocketHttpRequest _request;
        private readonly Action<string> _onMessage;
        private readonly Action _onClose;
        private readonly Action<byte[]> _onBinary;
        private readonly Action<byte[]> _onPing;
        private readonly Action<byte[]> _onPong;
        private byte[] _data;
        private int _dataLen;

        public Hybi13Handler(
            WebSocketHttpRequest request,
            Action<string> onMessage,
            Action onClose,
            Action<byte[]> onBinary,
            Action<byte[]> onPing,
            Action<byte[]> onPong)
        {
            _readState = new ReadState();
            _request = request;
            _onMessage = onMessage;
            _onClose = onClose;
            _onBinary = onBinary;
            _onPing = onPing;
            _onPong = onPong;
            _data = ArrayPool<byte>.Shared.Rent(1 * 1024 * 1024); // 1 MB read buffer
            _dataLen = 0;
        }

        public void Dispose()
        {
            if (_data != null)
            {
                ArrayPool<byte>.Shared.Return(_data);
                _data = null;
            }
        }

        public void Receive(Span<byte> newData)
        {
            if (newData.Length + _dataLen >= _data.Length)
                throw new WebSocketException(WebSocketStatusCodes.MessageTooBig);

            var dest = new Span<byte>(_data, _dataLen, newData.Length);
            newData.CopyTo(dest);
            _dataLen += newData.Length;

            ReceiveData();
        }

        public MemoryBuffer CreateHandshake()
        {
            FleckLog.Debug("Building Hybi-14 Response");

            var builder = StringBuilder.Value;
            builder.Clear();

            builder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");

            var responseKey =  CreateResponseKey(_request["Sec-WebSocket-Key"]);
            builder.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", responseKey);
            builder.Append("\r\n");

            var bytes = UTF8.GetBytes(builder.ToString());
            return new MemoryBuffer(bytes, bytes.Length);
        }

        public MemoryBuffer FrameText(string text)
        {
            return FrameData(UTF8.GetBytes(text), FrameType.Text);
        }

        public MemoryBuffer FrameBinary(byte[] bytes)
        {
            return FrameData(bytes, FrameType.Binary);
        }

        public MemoryBuffer FramePing(byte[] bytes)
        {
            return FrameData(bytes, FrameType.Ping);
        }

        public MemoryBuffer FramePong(byte[] bytes)
        {
            return FrameData(bytes, FrameType.Pong);
        }

        public unsafe MemoryBuffer FrameClose(ushort code)
        {
            var codeSpan = new Span<byte>(&code, sizeof(ushort));
            codeSpan.Reverse();
            return FrameData(codeSpan, FrameType.Close);
        }
        
        private MemoryBuffer FrameData(Span<byte> payload, FrameType frameType)
        {
            var data = ArrayPool<byte>.Shared.Rent(payload.Length + 16);
            var writer = new SpanWriter(data);

            byte op = (byte)((byte)frameType + 128);
            writer.Write(op);
            
            if (payload.Length > ushort.MaxValue) {
                writer.Write<byte>(127);
                writer.Write((ulong)payload.Length);
            } else if (payload.Length > 125) {
                writer.Write<byte>(126);
                writer.Write((ushort)payload.Length);
            } else {
                writer.Write((byte)payload.Length);
            }
            
            writer.Write(payload);

            return new MemoryBuffer(data, writer.Length);
        }
        
        private void ReceiveData()
        {
            while (_dataLen >= 2)
            {
                var isFinal = (_data[0] & 128) != 0;
                var reservedBits = (_data[0] & 112);
                var frameType = (FrameType)(_data[0] & 15);
                var isMasked = (_data[1] & 128) != 0;
                var length = (_data[1] & 127);
                
                
                if (!isMasked
                    || !frameType.IsDefined()
                    || reservedBits != 0 // Must be zero per spec 5.2
                    || (frameType == FrameType.Continuation && !_readState.FrameType.HasValue))
                    throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                
                var index = 2;
                int payloadLength;
                
                if (length == 127)
                {
                    if (_dataLen < index + 8)
                        return; //Not complete
                    payloadLength = new Span<byte>(_data, index, 8).ToLittleEndianInt();
                    index += 8;
                }
                else if (length == 126)
                {
                    if (_dataLen < index + 2)
                        return; //Not complete
                    payloadLength = new Span<byte>(_data, index, 2).ToLittleEndianInt();
                    index += 2;
                }
                else
                {
                    payloadLength = length;
                }
                
                if (_dataLen < index + 4) 
                    return; //Not complete
               
                var maskBytes = new Span<byte>(_data, index, 4);
                index += 4;
                
                if (_dataLen < index + payloadLength) 
                    return; //Not complete
                
                byte[] payloadData = new byte[payloadLength];
                for (int i = 0; i < payloadLength; i++)
                    payloadData[i] = (byte)(_data[index+i] ^ maskBytes[i % 4]);

                _readState.Data.AddRange(payloadData);

                var bytesUsed = index + payloadLength;
                Buffer.BlockCopy(_data, bytesUsed, _data, 0, _dataLen - bytesUsed);
                _dataLen -= index + payloadLength;
                
                if (frameType != FrameType.Continuation)
                    _readState.FrameType = frameType;
                
                if (isFinal && _readState.FrameType.HasValue)
                {
                    var stateData = _readState.Data.ToArray();
                    var stateFrameType = _readState.FrameType;
                    _readState.Clear();
                    
                    ProcessFrame(stateFrameType.Value, stateData);
                }
            }
        }
        
        private void ProcessFrame(FrameType frameType, byte[] data)
        {
            switch (frameType)
            {
            case FrameType.Close:
                if (data.Length == 1 || data.Length > 125)
                    throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                    
                if (data.Length >= 2)
                {
                    var closeCode = (ushort)new Span<byte>(data, 0, 2).ToLittleEndianInt();
                    if (!WebSocketStatusCodes.ValidCloseCodes.Contains(closeCode) && (closeCode < 3000 || closeCode > 4999))
                        throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                }
                
                if (data.Length > 2)
                    ReadUTF8PayloadData(data.Skip(2).ToArray());
                
                _onClose();
                break;
            case FrameType.Binary:
                _onBinary(data);
                break;
            case FrameType.Ping:
                _onPing(data);
                break;
            case FrameType.Pong:
                _onPong(data);
                break;
            case FrameType.Text:
                _onMessage(ReadUTF8PayloadData(data));
                break;
            default:
                FleckLog.Debug("Received unhandled " + frameType);
                break;
            }
        }
        
        internal static string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var bytes = SHA1.ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }
        
        internal static string ReadUTF8PayloadData(byte[] bytes)
        {
            try
            {
                return UTF8.GetString(bytes);
            }
            catch(ArgumentException)
            {
                throw new WebSocketException(WebSocketStatusCodes.InvalidFramePayloadData);
            }
        }
    }
}
