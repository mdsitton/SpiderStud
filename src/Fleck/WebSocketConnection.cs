using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Net.Sockets;
using Fleck.Helpers;
using BinaryEx;

namespace Fleck
{
    public class WebSocketConnection : IWebSocketConnection
    {
        private const int ReadSize = 8 * 1024;

        public ISocket Socket { get; set; }
        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }
        public bool IsAvailable => !closing && !closed && Socket.Connected;

        public IWebSocketDataHandler DataHandler { get; private set; }
        private byte[] receiveBuffer;
        private int receiveOffset;
        private bool closing;
        private bool closed;
        private bool handshakeCompleted = false;

        public WebSocketConnection(
            ISocket socket,
            IWebSocketDataHandler dataHandler)
        {
            Socket = socket;
            DataHandler = dataHandler;
        }

        const int MaxStackLimit = 1024;


        private static Span<byte> EnsureSize(byte[]? data, int byteCount)
        {

            // Return allocated arraypool data
            if (data?.Length < byteCount)
            {
                ArrayPool<byte>.Shared.Return(data);
                data = null;
            }

            if (data == null)
            {
                data = ArrayPool<byte>.Shared.Rent(byteCount);
            }
            return new Span<byte>(data, 0, byteCount);
        }

        // byte[]? strTmpData = null;

        // /// <summary>
        // /// Send string as utf-8 bytes without memory allocation
        // /// </summary>
        // /// <param name="message"></param>
        // public void Send(string message)
        // {
        //     int byteCount = Encoding.UTF8.GetByteCount(message);

        //     Span<byte> spanLocation = EnsureSize(strTmpData, byteCount);
        //     var realBytes = Encoding.UTF8.GetBytes(message, spanLocation);

        //     SendText(spanLocation.Slice(0, realBytes));
        // }

        byte[]? tmpPayloadData = null;


        public void SendMessage(FrameType frameType, ReadOnlySpan<byte> payload, bool endOfMessage = true)
        {
            if (!handshakeCompleted)
                throw new InvalidOperationException("Cannot send before handshake");

            if (!IsAvailable)
            {
                FleckLog.Warn("Data sent while closing or after close. Ignoring.");
                return;
            }

            Span<byte> dataOut = stackalloc byte[1024];
            int writtenSize = Handlers.Hybi13Handler.WriteFrame(dataOut, payload, frameType, endOfMessage);

            SendBytes(dataOut);
        }

        public void Close(ushort code = WebSocketStatusCodes.NormalClosure)
        {
            if (closing || closed)
                return;

            closing = true;

            if (!handshakeCompleted || !Socket.Connected)
            {
                CloseSocket();
                return;
            }

            Span<byte> dataIn = stackalloc byte[2];
            dataIn.WriteUInt16BE(0, code);

            Span<byte> dataOut = stackalloc byte[32];

            int writtenSize = Handlers.Hybi13Handler.WriteFrame(dataOut, dataIn, FrameType.Close);
            dataOut = dataOut.Slice(0, writtenSize);

            if (dataOut.Length == 0)
            {
                CloseSocket();
                return;
            }

            SendBytes(dataOut);
            CloseSocket();
        }

        public bool CreateHandler(ReadOnlySpan<byte> data)
        {
            var request = RequestParser.Parse(data);
            if (request == null)
                return false;

            if (!request.Headers.TryGetValue("Sec-WebSocket-Version", out string version) || version != "13")
            {
                throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
            }

            var ip = Socket.RemoteIpAddress;
            var port = Socket.RemotePort;

            if (ip is null || port is null)
            {
                return false;
            }

            ConnectionInfo = WebSocketConnectionInfo.Create(request, ip, port.Value);

            var handshake = Handlers.Hybi13Handler.CreateHandshake(request);
            if (SendBytes(handshake))
            {
                DataHandler.OnOpen();
                return true;
            }

            return false;
        }

        public void Receiving()
        {
            if (!IsAvailable)
                return;

            if (receiveBuffer == null)
                receiveBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);

            Receive(receiveBuffer, 0);
        }

        private void HandleReadError(Exception e)
        {
            if (e is AggregateException)
            {
                var agg = e as AggregateException;
                HandleReadError(agg.InnerException);
                return;
            }

            if (e is ObjectDisposedException)
            {
                FleckLog.Warn("Swallowing ObjectDisposedException", e);
                return;
            }

            OnError(e);

            if (e is WebSocketException)
            {
                FleckLog.Debug("Error while reading", e);
                Close(((WebSocketException)e).StatusCode);
            }
            else if (e is IOException)
            {
                FleckLog.Debug("Error while reading", e);
                Close(WebSocketStatusCodes.AbnormalClosure);
            }
            else
            {
                FleckLog.Error("Application Error", e);
                Close(WebSocketStatusCodes.InternalServerError);
            }
        }

        private void Receive(Span<byte> data)
        {
            try
            {
                // Don't read from the socket if there is no data available
                // This is so we don't block unneccesarilly 
                if (Socket.BytesAvailable == 0)
                {
                    return;
                }
                var bytesRead = Socket.Read(data);

                if (bytesRead <= 0)
                {
                    FleckLog.Debug("0 bytes read. Closing.");
                    CloseSocket();
                    return;
                }

                var readBytes = data.Slice(0, bytesRead);

                if (Handler != null)
                {
                    Handler.Receive(readBytes);
                }
                else
                {
                    receiveOffset += bytesRead;
                    Span<byte> buffer = new Span<byte>(receiveBuffer, 0, receiveOffset);
                    var started = CreateHandler(buffer);
                    Receive(receiveBuffer, started ? 0 : receiveOffset);
                }
            }
            catch (Exception e)
            {
                HandleReadError(e);
            }
        }

        private void HandleWriteError(Exception e)
        {
            if (e is IOException)
                FleckLog.Debug("Failed to send. Disconnecting.", e);
            else
                FleckLog.Info("Failed to send. Disconnecting.", e);

            OnError(e);
            CloseSocket();
        }

        private bool SendBytes(ReadOnlySpan<byte> bytes)
        {
            try
            {
                Socket.Write(bytes);
                return true;
            }
            catch (Exception e)
            {
                HandleWriteError(e);
                return false;
            }
        }

        private void CloseSocket()
        {
            closing = true;
            OnClose();
            closed = true;
            Socket.Close();
            Socket.Dispose();
            closing = false;

            if (receiveBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                receiveBuffer = null;
            }
        }
    }
}
