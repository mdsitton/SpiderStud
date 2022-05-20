using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;
using SpiderStud.Interfaces;
using SpiderStud.Tls;

namespace SpiderStud.Http
{
    public class HttpConnection : IAsyncSocketHandler, IBcTlsCallbacks
    {
        private const int ReadSize = 8 * 1024;

        private bool isClosing = false;
        private readonly Socket clientSocket;

        public SocketAsyncArgs sendEventArgs;
        public SocketAsyncArgs receiveEventArgs;

        public Socket ClientSocket => clientSocket;
        public SecureIPEndpoint? ConnectedEndpoint { get; private set; }

        public bool IsAvailable => ClientSocket.Connected && !isClosing;

        private SpiderStudServer server;

        // Tls support
        private BcTlsServer? tlsServer;
        private TlsServerProtocol? tlsProtocol;
        private bool tlsHandshakeComplete = false;
        private bool tlsEnabled = false;

        public HttpConnection(SpiderStudServer server)
        {
            this.server = server;
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            // sendEventArgs will also be used for closing connection
            sendEventArgs = new SocketAsyncArgs(this);
            sendEventArgs.DisconnectReuseSocket = true;
            receiveEventArgs = new SocketAsyncArgs(this);
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
                OnReceive(clientSocket, sendEventArgs);
            }
        }

        public void CloseConnection()
        {
            if (isClosing) return;

            isClosing = true;
            while (!clientSocket.DisconnectAsync(sendEventArgs))
            {
                OnDisconnect(clientSocket, sendEventArgs);
            }
        }

        public void Send(Span<byte> data)
        {
            if (isClosing) return;

            // todo copy data to persistant memory object and attach to eventArgs

            while (!clientSocket.SendAsync(sendEventArgs))
            {
                OnDisconnect(clientSocket, sendEventArgs);
            }
        }

        public void OnDisconnect(Socket socket, SocketAsyncArgs e)
        {

            tlsEnabled = false;
            tlsServer = null;
            tlsProtocol = null;
        }

        public void OnReceive(Socket socket, SocketAsyncArgs e)
        {

        }

        // Called when send is completed
        public void OnSend(Socket socket, SocketAsyncArgs e)
        {
        }

        public void OnError(Socket socket, SocketError error, SocketAsyncArgs e)
        {
            CloseConnection();
        }
    }
}