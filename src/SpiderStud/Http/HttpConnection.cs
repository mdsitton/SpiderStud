using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;
using SpiderStud.Interfaces;
using SpiderStud.Tls;

namespace SpiderStud.Http
{
    public class HttpConnection : IAsyncSocketHandler, IBcTlsCallbacks, IDisposable
    {
        // Chose a size of 8000 bytes for our buffers
        // This is because after this size on mono buffers get moved to the LOH
        // For coreclr this limit much larger at 85k
        private const int BufferSize = 8000;

        private bool isClosing = false;
        private readonly Socket clientSocket;

        public SocketAsyncArgs sendEventArgs;
        public SocketAsyncArgs receiveEventArgs;

        public IMemoryOwner<byte> sendArgsBuffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        public IMemoryOwner<byte> receiveArgsBuffer = MemoryPool<byte>.Shared.Rent(BufferSize);


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
            sendEventArgs.SetBuffer(sendArgsBuffer.Memory);
            receiveEventArgs = new SocketAsyncArgs(this);
            receiveEventArgs.SetBuffer(receiveArgsBuffer.Memory);
        }

        public void Dispose()
        {
            sendArgsBuffer.Dispose();
            receiveArgsBuffer.Dispose();
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
            // TODO - trigger receiving on client socket
            // StartReceiving();
            while (!clientSocket.ReceiveAsync(sendEventArgs))
            {
                OnReceiveComplete(clientSocket, sendEventArgs, sendEventArgs.TransferedData);
            }
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

        public void Send(Span<byte> data)
        {
            if (isClosing) return;

            // todo copy data to persistant memory object and attach to eventArgs

            while (!clientSocket.SendAsync(sendEventArgs))
            {
                OnDisconnectComplete(clientSocket, sendEventArgs);
            }
        }

        public void OnDisconnectComplete(Socket socket, SocketAsyncArgs e)
        {

            tlsEnabled = false;
            tlsServer = null;
            tlsProtocol = null;
        }

        public void OnReceiveComplete(Socket socket, SocketAsyncArgs e, ReadOnlySpan<byte> receivedData)
        {

        }

        // Called when send is completed
        public void OnSendComplete(Socket socket, SocketAsyncArgs e)
        {
        }

        public void OnError(Socket socket, SocketError error, SocketAsyncArgs e)
        {
            CloseConnection();
        }
    }
}