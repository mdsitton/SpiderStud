using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SpiderStud.Interfaces
{
    public interface IAsyncSocketHandler
    {
        void OnDisconnect(Socket socket, SocketAsyncArgs e);
        void OnReceive(Socket socket, SocketAsyncArgs e);
        void OnSend(Socket socket, SocketAsyncArgs e);
        void OnError(Socket socket, SocketError error, SocketAsyncArgs e);
    }
}