using System;
using System.Threading.Tasks;

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
        Task Send(string message);
        Task Send(byte[] message);
        Task SendPing(byte[] message);
        Task SendPong(byte[] message);
        void Close();
        IWebSocketConnectionInfo ConnectionInfo { get; }
        bool IsAvailable { get; }
    }
}
