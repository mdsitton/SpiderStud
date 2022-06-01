using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SpiderStud.Interfaces
{
    public interface IAsyncSocketHandler
    {
        bool IsAvailable { get; }
        void OnDisconnectComplete(Socket socket, SocketAsyncArgs e);
        void OnReceiveComplete(Socket socket, SocketAsyncArgs e, int dataWritten);
        Memory<byte> GetRecieveMemory(int size);
        void OnSendComplete(Socket socket, SocketAsyncArgs e);
        void OnError(Socket socket, SocketError error, SocketAsyncArgs e);
    }
}