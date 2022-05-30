using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        private volatile bool isClosing = false;
        private readonly Socket clientSocket;

        public SocketAsyncArgs sendEventArgs;
        public SocketAsyncArgs receiveEventArgs;
        public SocketAsyncArgs disconnectEventArgs;

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

        private DateTime lastReceiveTime = DateTime.UtcNow;

        private bool connectionProtocolUpgraded = false;

        public HttpConnection(SpiderStudServer server)
        {
            this.server = server;
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.LingerState = lingerState;

            // sendEventArgs will also be used for closing connection
            sendEventArgs = new SocketAsyncArgs(this, clientSocket);
            receiveEventArgs = new SocketAsyncArgs(this, clientSocket);
            disconnectEventArgs = new SocketAsyncArgs(this, clientSocket);
            disconnectEventArgs.DisconnectReuseSocket = true;
        }

        public void Dispose()
        {
            sendBuffer.Dispose();
            receiveBuffer.Dispose();
        }

        public void InitConnection(SecureIPEndpoint endpoint)
        {
            ConnectedEndpoint = endpoint;
            isClosing = false;
        }

        public void OnHandshakeComplete()
        {
            tlsHandshakeComplete = true;
        }

        // called by server timer polling thread so we can check
        // for connection timeouts and close the connection
        internal void TimerTick()
        {
            var currentTime = DateTime.UtcNow;

            TimeSpan diff = currentTime - lastReceiveTime;

            if (diff > server.HttpTimeoutSpan)
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Once a protocol upgrade has been triggered no further http parsing will occur on this connection
        /// </summary>
        public void TriggerProtocolUpgrade()
        {
            connectionProtocolUpgraded = true;
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
            lastReceiveTime = DateTime.UtcNow;

            sendEventArgs.StartReceive(clientSocket);
        }

        public void CloseConnection()
        {
            if (isClosing) return;

            isClosing = true;
            disconnectEventArgs.StartDisconnect(clientSocket);
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
            sendState = SEND_ACTIVE;
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
            CommitSend();
        }

        public void SendResponse(HttpResponse response, string? responseBody = null)
        {
            int bodyLength = 0;
            if (responseBody != null)
            {
                bodyLength = Encoding.UTF8.GetByteCount(responseBody);
                response.Headers["Content-Type"] = "text/plain; charset=UTF-8";
                response.Headers["Content-Length"] = bodyLength.ToString();
            }
            IBufferWriter<byte>? writer;

            // wait until any active sending data is completed
            while (!TryInitSend(out writer))
            {
                Thread.Sleep(1);
            }
            response.WriteResponseHeader(writer!);

            // Write response body
            if (responseBody != null)
            {
                Span<byte> tmpWrite = stackalloc byte[bodyLength];
                Encoding.UTF8.GetBytes(responseBody, tmpWrite);
                writer!.Write(tmpWrite);
                writer!.Advance(bodyLength);
            }

            CommitSend();
        }

        // Dictionary that is reused for processing headers
        private Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public Memory<byte> GetRecieveMemory(int size)
        {
            Logging.Info($"Receive requested {size} bytes of memory");
            return receiveBuffer.GetMemory(size);
        }

        public void OnReceiveComplete(Socket socket, SocketAsyncArgs e, int dataWritten)
        {
            Logging.Info($"Receive complete {dataWritten} bytes");
            receiveBuffer.Advance(dataWritten);
            lastReceiveTime = DateTime.UtcNow;
            var currentWritten = receiveBuffer.WrittenSequence;
            long offset = HttpRequest.EndOfHeaderIndex(currentWritten);
            if (offset != -1)
            {
                if (currentWritten.IsSingleSegment)
                {
                    headers.Clear(); // clear headers obj
                    HttpRequest request = new HttpRequest(headers);
                    request.Parse(currentWritten.FirstSpan);

                    // if (request.Headers.TryGetValue("Content-Length", out string lengthStr) &&
                    //     int.TryParse(lengthStr, NumberStyles.None, null, out int length))
                    // {


                    // }
                    var requestHandler = server.GetService(request.Path);
                    if (requestHandler == null)
                    {
                        // Endpoint not found return error
                        HttpResponse response = new HttpResponse(HttpStatusCode.NotFound);
                        SendResponse(response);
                        CloseConnection();
                    }
                    else
                    {
                        bool keepConnectionAlive = requestHandler.OnRequest(request, this);
                        if (!keepConnectionAlive)
                        {
                            CloseConnection();
                        }
                    }
                }
            }
        }

        // Called when send is completed
        public void OnSendComplete(Socket socket, SocketAsyncArgs e)
        {
            Interlocked.Exchange(ref sendState, SEND_IDLE);
        }

        public void OnError(Socket socket, SocketError error, SocketAsyncArgs e)
        {
            Logging.Info($"Error {error} closing");
            CloseConnection();
        }

        public void OnDisconnectComplete(Socket socket, SocketAsyncArgs e)
        {
            Logging.Info("Closing connection");
            tlsEnabled = false;
            tlsServer = null;
            tlsProtocol = null;
            server.OnClientDisconnect(this);
            isClosing = false;
            // Clear any data segments
            receiveBuffer.Reset();
        }

    }
}