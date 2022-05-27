using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;
using SpiderStud.Buffers;
using SpiderStud.Interfaces;
using SpiderStud.Tls;

namespace SpiderStud.Http
{
    public class HttpConnection : IAsyncSocketHandler, IBcTlsCallbacks, IDisposable
    {
        private bool isClosing = false;
        private readonly Socket clientSocket;

        public SocketAsyncArgs sendEventArgs;
        public SocketAsyncArgs receiveEventArgs;

        public SequenceWriter sendBuffer = new SequenceWriter();
        public SequenceWriter receiveBuffer = new SequenceWriter();

        public Socket ClientSocket => clientSocket;
        public SecureIPEndpoint? ConnectedEndpoint { get; private set; }

        public bool IsAvailable => ClientSocket.Connected && !isClosing;

        private SpiderStudServer server;

        // For sockets we want to make sure all data is delivered once closed
        private static LingerOption lingerState = new LingerOption(true, 0);

        // Tls support
        private BcTlsServer? tlsServer;
        private TlsServerProtocol? tlsProtocol;
        private bool tlsHandshakeComplete = false;
        private bool tlsEnabled = false;

        public HttpConnection(SpiderStudServer server)
        {
            this.server = server;
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.LingerState = lingerState;

            // sendEventArgs will also be used for closing connection
            sendEventArgs = new SocketAsyncArgs(this);
            sendEventArgs.DisconnectReuseSocket = true;
            receiveEventArgs = new SocketAsyncArgs(this);
        }

        public void Dispose()
        {
            sendBuffer.Dispose();
            receiveBuffer.Dispose();
        }

        public void InitConnection(SecureIPEndpoint endpoint)
        {
            ConnectedEndpoint = endpoint;
        }

        public void OnHandshakeComplete()
        {
            tlsHandshakeComplete = true;
        }

        private void InitTls()
        {
            if (ConnectedEndpoint != null && ConnectedEndpoint.Secure)
            {
                if (server.config.Certificate == null)
                {
                    throw new InvalidOperationException("Secure WebSocket must have certificates defined");
                }

                Logging.Debug("Authenticating Secure Connection");

                tlsServer = new BcTlsServer(this, server.config.EnabledTlsVersions, server.config.Certificate);
                tlsProtocol = new TlsServerProtocol();
                // Begin waiting for TLS handshake in non-blocking mode
                tlsProtocol.Accept(tlsServer);
                tlsEnabled = true;
            }
        }

        public void StartReceiving()
        {
            if (isClosing) return;

            InitTls();

            sendEventArgs.StartReceive(clientSocket);
        }

        public void CloseConnection()
        {
            if (isClosing) return;

            isClosing = true;
            // Disable send/receieve and ensure data is sent correctly
            clientSocket.Shutdown(SocketShutdown.Both);
            while (!clientSocket.DisconnectAsync(sendEventArgs))
            {
                OnDisconnectComplete(clientSocket, sendEventArgs);
            }
        }

        /// <summary>
        /// Try to secure socket sending, if sending is currently active or the socket is closing this will return false
        /// </summary>
        /// <param name="writer">writer instance to write data</param>
        public bool TryInitSend(out IBufferWriter<byte>? writer)
        {
            if (isClosing || sendState == SEND_ACTIVE)
            {
                writer = null;
                return false;
            }

            writer = sendBuffer;
            return true;
        }

        const int SEND_ACTIVE = 1;
        const int SEND_IDLE = 0;

        int sendState = SEND_IDLE;

        public void CommitSend()
        {
            // var sequence = sendBuffer.SequenceCommit();

            // TODO - implement TLS

            // if (tlsEnabled)
            // {
            //     tlsProtocol.ReadInput
            // }

            sendEventArgs.StartSend(clientSocket, sendBuffer.SequenceCommit());
        }

        /// <summary>
        /// Blocking send operation
        /// </summary>
        /// <param name="data">Data to send over the connection</param>
        public void Send(Span<byte> data)
        {
            if (isClosing) return;

            IBufferWriter<byte>? writer;

            // wait until any active sending data is completed
            while (!TryInitSend(out writer))
            {
                Thread.Sleep(1);
            }

            // Copy data to eventArgs
            data.CopyTo(writer!.GetSpan(data.Length));
            writer.Advance(data.Length);
            sendState = SEND_ACTIVE;
            CommitSend();
        }

        public void OnDisconnectComplete(Socket socket, SocketAsyncArgs e)
        {
            tlsEnabled = false;
            tlsServer = null;
            tlsProtocol = null;
        }

        public Memory<byte> GetRecieveMemory(int size)
        {
            return receiveBuffer.GetMemory(size);
        }

        public void OnReceiveComplete(Socket socket, SocketAsyncArgs e, int dataWritten)
        {
            receiveBuffer.Advance(dataWritten);
        }

        // Called when send is completed
        public void OnSendComplete(Socket socket, SocketAsyncArgs e)
        {
            Interlocked.Exchange(ref sendState, SEND_IDLE);
        }

        public void OnError(Socket socket, SocketError error, SocketAsyncArgs e)
        {
            CloseConnection();
        }
    }
}