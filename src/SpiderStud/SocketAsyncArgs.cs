using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using SpiderStud.Interfaces;

namespace SpiderStud
{
    public class SocketAsyncArgs : SocketAsyncEventArgs
    {
        readonly IAsyncSocketHandler completionhandler;
        public SocketAsyncArgs(IAsyncSocketHandler handler)
        {
            completionhandler = handler;
        }

        public Span<byte> TransferedData => MemoryBuffer.Span.Slice(Offset, BytesTransferred);

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                completionhandler.OnError(e.ConnectSocket, e.SocketError, this);
            }

            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    // re-schedule next read and handle any syncrously completed events
                    do
                    {
                        completionhandler.OnReceiveComplete(e.ConnectSocket, this, TransferedData);
                    }
                    while (!ConnectSocket.ReceiveAsync(this));
                    break;
                case SocketAsyncOperation.Send:
                    // TODO - Automatic segmented buffersending?
                    // // re-schedule next write and handle any syncrously completed events
                    // do
                    // {
                    // }
                    // while (!ConnectSocket.SendAsync(this));
                    completionhandler.OnSendComplete(e.ConnectSocket, this);
                    break;
                case SocketAsyncOperation.Disconnect:
                    completionhandler.OnDisconnectComplete(e.ConnectSocket, this);
                    break;
            }
        }
    }
}