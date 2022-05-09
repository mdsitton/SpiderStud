using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Net.Sockets;
using SpiderStud.Helpers;
using BinaryEx;
using SpiderStud.Http;

namespace SpiderStud
{
    public class WebSocketConnection : IWebSocketConnection
    {
        private const int ReadSize = 8 * 1024;

        public ISocket Socket { get; set; }
        public IWebSocketConnectionInfo? ConnectionInfo { get; private set; }
        public bool IsAvailable => !closing && !closed && Socket.Connected;

        public IWebSocketServiceHandler DataHandler { get; private set; }
        private byte[] receiveBuffer;
        private int receiveOffset;
        private bool closing;
        private bool closed;
        private bool handshakeCompleted = false;

        public WebSocketConnection(ISocket socket, WebSocketServer server, IWebSocketServiceHandler dataHandler)
        {
            Socket = socket;
            DataHandler = dataHandler;
            receiveBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);
            dataHandler.OnConfig(server, this);
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
                SpiderStudLog.Warn("Data sent while closing or after close. Ignoring.");
                return;
            }

            Span<byte> dataOut = stackalloc byte[1024];
            int writtenSize = FrameParsing.WriteFrame(dataOut, payload, frameType, endOfMessage);

            SendBytes(dataOut);
        }

        public void Close(WebSocketStatusCode code = WebSocketStatusCode.NormalClosure)
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
            dataIn.WriteUInt16BE(0, (ushort)code);

            Span<byte> dataOut = stackalloc byte[32];

            int writtenSize = FrameParsing.WriteFrame(dataOut, dataIn, FrameType.Close);
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
            var request = HttpHeader.Parse(data);
            if (request == null)
                return false;

            if (!request.Headers.TryGetValue("Sec-WebSocket-Version", out string version) || version != "13")
            {
                // TODO - Return http 400 error since we have not established a websocket connection with the handshake
                throw new WebSocketException(WebSocketStatusCode.ProtocolError);
            }

            var ip = Socket.RemoteIpAddress;
            var port = Socket.RemotePort;

            if (ip is null || port is null)
            {
                return false;
            }

            ConnectionInfo = WebSocketConnectionInfo.Create(request, ip, port.Value);

            var handshake = HttpHeader.CreateHandshake(request);
            if (SendBytes(handshake))
            {
                DataHandler.OnOpen();
                return true;
            }

            return false;
        }

        public void Update()
        {

            if (!IsAvailable)
                return;

            Receive(receiveBuffer);
        }

        private void HandleReadError(Exception e)
        {
            if (e is AggregateException agg)
            {
                HandleReadError(agg.InnerException);
                return;
            }

            if (e is ObjectDisposedException)
            {
                SpiderStudLog.Warn("Swallowing ObjectDisposedException", e);
                return;
            }

            DataHandler.OnError(e);

            if (e is WebSocketException wse)
            {
                SpiderStudLog.Debug("Error while reading", e);
                Close(wse.StatusCode);
            }
            else if (e is IOException)
            {
                SpiderStudLog.Debug("Error while reading", e);
                Close(WebSocketStatusCode.AbnormalClosure);
            }
            else
            {
                SpiderStudLog.Error("Application Error", e);
                Close(WebSocketStatusCode.InternalServerError);
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
                    SpiderStudLog.Debug("0 bytes read. Closing.");
                    CloseSocket();
                    return;
                }

                var readBytes = data.Slice(0, bytesRead);

                // TODO - rewrite handler code
                // if (Handler != null)
                // {
                //     FrameParsing.Receive(readBytes);
                //     DispatchFrameHandler();
                // }
                // else
                // {
                //     receiveOffset += bytesRead;
                //     Span<byte> buffer = new Span<byte>(receiveBuffer, 0, receiveOffset);
                //     var started = CreateHandler(buffer);
                //     Receive(new Span<byte>(receiveBuffer, started ? 0 : receiveOffset, receiveBuffer.Length));
                // }
            }
            catch (Exception e)
            {
                HandleReadError(e);
            }
        }

        private void DispatchFrameHandler(FrameType frameType, bool endOfMessage, ReadOnlySpan<byte> frameData)
        {
            switch (frameType)
            {
                case FrameType.Close:
                    if (frameData.Length == 1 || frameData.Length > 125)
                        throw new WebSocketException(WebSocketStatusCode.ProtocolError);

                    if (frameData.Length >= 2)
                    {
                        var closeCode = (WebSocketStatusCode)frameData.ReadUInt16BE(0);
                        if (!closeCode.IsValidCode())
                        {
                            throw new WebSocketException(WebSocketStatusCode.ProtocolError);
                        }
                    }

                    DataHandler.OnClose();
                    break;
                case FrameType.Binary:
                case FrameType.Ping:
                case FrameType.Pong:
                case FrameType.Text:
                    DataHandler.OnMessage(frameType, endOfMessage, frameData);
                    break;
                default:
                    SpiderStudLog.Debug("Received unhandled " + frameType);
                    break;
            }
        }

        private void HandleWriteError(Exception e)
        {
            if (e is IOException)
                SpiderStudLog.Debug("Failed to send. Disconnecting.", e);
            else
                SpiderStudLog.Info("Failed to send. Disconnecting.", e);

            DataHandler.OnError(e);
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
            DataHandler.OnClose();
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
