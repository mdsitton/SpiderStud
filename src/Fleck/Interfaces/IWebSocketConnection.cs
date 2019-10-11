using System;

namespace Fleck
{
    public delegate void BinaryDataHandler(ArraySegment<byte> data);

    public interface IWebSocketConnection
    {
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }
        BinaryDataHandler OnBinary { get; set; }
        BinaryDataHandler OnPing { get; set; }
        BinaryDataHandler OnPong { get; set; }
        Action<Exception> OnError { get; set; }
        void Send(string message);
        void Send(ArraySegment<byte> message);
        void SendPing(ArraySegment<byte> message);
        void SendPong(ArraySegment<byte> message);
        void Close();
        IWebSocketConnectionInfo ConnectionInfo { get; }
        bool IsAvailable { get; }
    }
}
