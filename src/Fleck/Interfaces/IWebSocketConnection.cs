using System;

namespace Fleck
{
    public interface IWebSocketConnection
    {
        IWebSocketClientHandler DataHandler { get; }

        void SendMessage(FrameType type, ReadOnlySpan<byte> data, bool endOfMessage = true);
        void Close(StatusCode statusCode);
        IWebSocketConnectionInfo? ConnectionInfo { get; }
        bool IsAvailable { get; }
    }
}
