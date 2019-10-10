using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Fleck
{
    public class WebSocketConnection : IWebSocketConnection
    {
        private const int ReadSize = 8 * 1024;

        public ISocket Socket { get; set; }
        public IHandler Handler { get; set; }
        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnMessage { get; set; }
        public BinaryDataHandler OnBinary { get; set; }
        public BinaryDataHandler OnPing { get; set; }
        public BinaryDataHandler OnPong { get; set; }
        public Action<Exception> OnError { get; set; }
        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }
        public bool IsAvailable => !_closing && !_closed && Socket.Connected;

        private readonly Action<IWebSocketConnection> _initialize;
        private readonly Func<IWebSocketConnection, WebSocketHttpRequest, IHandler> _handlerFactory;
        private readonly Func<ArraySegment<byte>, WebSocketHttpRequest> _parseRequest;
        private byte[] _receiveBuffer;
        private int _receiveOffset;
        private bool _closing;
        private bool _closed;

        public WebSocketConnection(
            ISocket socket,
            Action<IWebSocketConnection> initialize,
            Func<ArraySegment<byte>, WebSocketHttpRequest> parseRequest,
            Func<IWebSocketConnection, WebSocketHttpRequest, IHandler> handlerFactory)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            OnBinary = x => { };
            OnPing = x => { };
            OnPong = x => { };
            OnError = x => { };
            _initialize = initialize;
            _handlerFactory = handlerFactory;
            _parseRequest = parseRequest;
        }

        public Task Send(string message)
        {
            return Send(Handler.FrameText(message));
        }

        public Task Send(ArraySegment<byte> message)
        {
            return Send(Handler.FrameBinary(message));
        }

        public Task SendPing(ArraySegment<byte> message)
        {
            return Send(Handler.FramePing(message));
        }

        public Task SendPong(ArraySegment<byte> message)
        {
            return Send(Handler.FramePong(message));
        }

        private Task Send(MemoryBuffer buffer)
        {
            if (Handler == null)
                throw new InvalidOperationException("Cannot send before handshake");

            if (!IsAvailable)
            {
                const string errorMessage = "Data sent while closing or after close. Ignoring.";
                FleckLog.Warn(errorMessage);

                var taskForException = new TaskCompletionSource<object>();
                taskForException.SetException(new ConnectionNotAvailableException(errorMessage));
                return taskForException.Task;
            }

            return SendBytes(buffer);
        }

        public void Close()
        {
            Close(WebSocketStatusCodes.NormalClosure);
        }

        public void Close(ushort code)
        {
            if (_receiveBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_receiveBuffer);
                _receiveBuffer = null;
            }

            if (!IsAvailable)
                return;

            _closing = true;

            if (Handler == null)
            {
                CloseSocket();
                return;
            }

            var bytes = Handler.FrameClose(code);
            if (bytes.Length == 0)
                CloseSocket();
            else
                SendBytes(bytes, CloseSocket);
        }

        public bool CreateHandler(ArraySegment<byte> data)
        {
            var request = _parseRequest(data);
            if (request == null)
                return false;

            Handler = _handlerFactory(this, request);
            if (Handler == null)
                return false;

            ConnectionInfo = WebSocketConnectionInfo.Create(request, Socket.RemoteIpAddress, Socket.RemotePort);

            _initialize(this);

            var handshake = Handler.CreateHandshake();
            SendBytes(handshake, OnOpen);
            return true;
        }

        public void StartReceiving()
        {
            if (!IsAvailable)
                return;

            if (_receiveBuffer == null)
                _receiveBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);

            Receive(_receiveBuffer, 0);
        }

        private void HandleReadSuccess(int bytesRead)
        {
            if (bytesRead <= 0)
            {
                FleckLog.Debug("0 bytes read. Closing.");
                CloseSocket();
                return;
            }

            FleckLog.Debug($"{bytesRead} bytes read");

            var readBytes = new Span<byte>(_receiveBuffer, 0, bytesRead);

            if (Handler != null)
            {
                Handler.Receive(readBytes);
                Receive(_receiveBuffer, 0);
            }
            else
            {
                _receiveOffset += bytesRead;
                var started = CreateHandler(new ArraySegment<byte>(_receiveBuffer, 0, _receiveOffset));
                Receive(_receiveBuffer, started ? 0 : _receiveOffset);
            }
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
                FleckLog.Debug("Swallowing ObjectDisposedException", e);
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

        private void Receive(byte[] buffer, int offset)
        {
            try
            {
                Socket.Stream.BeginRead(buffer, offset, buffer.Length - offset, result =>
                {
                    var instance = (WebSocketConnection)result.AsyncState;

                    try
                    {
                        var bytesRead = instance.Socket.Stream.EndRead(result);
                        instance.HandleReadSuccess(bytesRead);
                    }
                    catch (Exception e)
                    {
                        instance.HandleReadError(e);
                    }
                }, this);
            }
            catch (Exception e)
            {
                HandleReadError(e);
            }
        }

        private Task SendBytes(MemoryBuffer bytes, Action callback = null)
        {
            return Socket.Send(bytes, () =>
            {
                FleckLog.Debug("Sent " + bytes.Length + " bytes");
                bytes.Dispose();
                callback?.Invoke();
            }, e =>
            {
                if (e is IOException)
                    FleckLog.Debug("Failed to send. Disconnecting.", e);
                else
                    FleckLog.Info("Failed to send. Disconnecting.", e);

                bytes.Dispose();
                CloseSocket();
            });
        }

        private void CloseSocket()
        {
            _closing = true;
            OnClose();
            _closed = true;
            Socket.Close();
            Socket.Dispose();
            _closing = false;
        }
    }
}
