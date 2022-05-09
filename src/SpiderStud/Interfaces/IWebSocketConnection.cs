using System;

namespace SpiderStud
{
    public interface IWebSocketConnection
    {
        IWebSocketClientHandler DataHandler { get; }

        void SendMessage(FrameType type, ReadOnlySpan<byte> data, bool endOfMessage = true);
        void Close(WebSocketStatusCode statusCode = WebSocketStatusCode.NormalClosure);
        IWebSocketConnectionInfo? ConnectionInfo { get; }
        bool IsAvailable { get; }
    }
}
