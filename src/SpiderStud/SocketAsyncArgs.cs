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
        readonly Socket clientSocket;

        SequenceOwner sendSequence = default;

        public SocketAsyncArgs(IAsyncSocketHandler handler, Socket socket)
        {
            completionhandler = handler;
            clientSocket = socket;
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
                Logging.Debug("Send immediate");
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
            SetBuffer(completionhandler.GetRecieveMemory(8000));

            while (!socket.ReceiveAsync(this))
            {
                Logging.Debug("Receive immediate");
                if (SocketError != SocketError.Success || BytesTransferred == 0)
                {
                    Logging.Debug($"Error {SocketError} bytes read {BytesTransferred}");
                    return;
                }
                completionhandler.OnReceiveComplete(socket, this, BytesTransferred);
                SetBuffer(completionhandler.GetRecieveMemory(64)); // set buffer for next operation
            }
        }

        public void StartDisconnect(Socket socket)
        {
            // Disable send/receieve and ensure data is sent correctly
            socket.Shutdown(SocketShutdown.Both);
            while (!socket.DisconnectAsync(this))
            {
                Logging.Debug("Disconnect immediate");
                completionhandler.OnDisconnectComplete(socket, this);
            }
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            if (SocketError != SocketError.Success)
            {
                completionhandler.OnError(clientSocket, SocketError, this);
            }

            // determine which type of operation just completed and call the associated handler
            switch (LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    // re-schedule next read and handle any syncrously completed events
                    do
                    {
                        Logging.Debug("OnCompleted Receive immediate");
                        if (SocketError != SocketError.Success || BytesTransferred == 0)
                        {
                            Logging.Debug($"{SocketError} bytes read {BytesTransferred}");
                            return;
                        }
                        completionhandler.OnReceiveComplete(clientSocket, this, BytesTransferred);
                        SetBuffer(completionhandler.GetRecieveMemory(64)); // set buffer for next operation
                    }
                    while (!clientSocket.ReceiveAsync(this));
                    break;
                case SocketAsyncOperation.Send:
                    // re-schedule next write and handle any syncrously completed events
                    do
                    {
                        SetBufferToCurrentSegment();
                        sendSequence.AdvanceCurrentSegment();
                    }
                    while (sendSequence.Current != null && !clientSocket.SendAsync(this));

                    if (sendSequence.Current == null)
                    {
                        // Notify handler when the full sequence send has completed so it can send the next sequence
                        completionhandler.OnSendComplete(clientSocket, this);

                        // Dispose of sequence, clear instance, and return since there is no more data to send
                        sendSequence.Dispose();
                        sendSequence = default;
                    }
                    break;
                case SocketAsyncOperation.Disconnect:
                    completionhandler.OnDisconnectComplete(clientSocket, this);
                    break;
            }
        }
    }
}