using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SpiderStud.Buffers;
using SpiderStud.Interfaces;

namespace SpiderStud
{
    public class SocketAsyncArgs : SocketAsyncEventArgs
    {
        readonly IAsyncSocketHandler completionhandler;

        SequenceOwner sendSequence = default;

        public SocketAsyncArgs(IAsyncSocketHandler handler)
        {
            completionhandler = handler;
        }

        public Span<byte> TransferedData => MemoryBuffer.Span.Slice(Offset, BytesTransferred);

        private void SetBufferToCurrentSegment()
        {
            if (sendSequence.Current != null)
            {
                SetBuffer(sendSequence.Current.AsMemory);
            }
        }

        public void StartSend(Socket socket, SequenceOwner sequence)
        {
            sendSequence = sequence;
            SetBufferToCurrentSegment();
            sequence.AdvanceCurrentSegment();

            while (sendSequence.Current != null && !socket.SendAsync(this))
            {
                SetBufferToCurrentSegment();
                sendSequence.AdvanceCurrentSegment();
            }

            if (sendSequence.Current == null)
            {
                // Notify handler when the full sequence send has completed so it can send the next sequence
                completionhandler.OnSendComplete(socket, this);

                // Dispose of sequence, clear instance, and return since there is no more data to send
                sendSequence.Dispose();
                sendSequence = default;
            }
        }

        public void StartReceive(Socket socket)
        {
            // Set initial recieve buffer
            SetBuffer(completionhandler.GetRecieveMemory(4096));

            while (!socket.ReceiveAsync(this))
            {
                completionhandler.OnReceiveComplete(socket, this, BytesTransferred);
            }
        }

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
                        completionhandler.OnReceiveComplete(e.ConnectSocket, this, BytesTransferred);
                    }
                    while (!ConnectSocket.ReceiveAsync(this));
                    break;
                case SocketAsyncOperation.Send:
                    // re-schedule next write and handle any syncrously completed events
                    do
                    {
                        SetBufferToCurrentSegment();
                        sendSequence.AdvanceCurrentSegment();
                    }
                    while (sendSequence.Current != null && !ConnectSocket.SendAsync(this));

                    if (sendSequence.Current == null)
                    {
                        // Notify handler when the full sequence send has completed so it can send the next sequence
                        completionhandler.OnSendComplete(e.ConnectSocket, this);

                        // Dispose of sequence, clear instance, and return since there is no more data to send
                        sendSequence.Dispose();
                        sendSequence = default;
                    }
                    break;
                case SocketAsyncOperation.Disconnect:
                    completionhandler.OnDisconnectComplete(e.ConnectSocket, this);
                    break;
            }
        }
    }
}