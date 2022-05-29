using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using SpiderStud.Helpers;
using System.Threading;
using System.Collections.Generic;
using SpiderStud.Http;
using SpiderStud.Tls;

namespace SpiderStud
{
    public delegate IWebSocketServiceHandler WsServiceHandlerFactory();
    public delegate IHttpServiceHandler HttpServiceHandlerFactory();

    public class ServerConfig
    {
        public X509Certificate2? Certificate;
        public TlsVersions EnabledTlsVersions;

        public bool SocketRestartAfterListenError = true;
        public bool DualStackSupport = true;
        public int RequestBacklogSize = 100;

        public int MaxConnections = 256;

        /// <summary>
        /// Http connection timeout period in ms. An usused HttpConnection will be
        /// closed after this period if it is not being actively used by a client.
        /// After the timeout 408 Request Timeout server response will be sent.
        /// 
        /// Note: this does not apply to websocket upgraded http requests <see cref="WsConnectionTimeout"/>
        /// </summary>
        public int HttpConnectionTimeout = 60000;

        /// <summary>
        /// Websocket connection timeout period in ms. Any websocket connection will be
        /// closed after this period if the client has not sent any data.
        /// A pong frame may be sent from clients unsolicited as a keep-alive
        /// forthe connection.
        /// </summary>
        public int WsConnectionTimeout = 60000;

        /// <summary>
        /// Server endpoints where client connections will be listened from
        /// The endpoints define ip address, port, and if a Secure socket is used 
        /// </summary>
        public List<SecureIPEndpoint> Endpoints = new List<SecureIPEndpoint>();
    }

    public partial class SpiderStudServer : IDisposable
    {
        protected List<Socket> EndpointSockets = new List<Socket>();
        public bool SupportsDualStack => config.DualStackSupport;
        public IReadOnlyList<SecureIPEndpoint> Endpoints => config.Endpoints;
        public bool SocketRestartAfterListenError => config.SocketRestartAfterListenError;
        public bool IsSecureSupported => config.Certificate != null;

        // We use a Queue of connections so that reused sockets have time to linger and fully close
        private readonly Queue<HttpConnection> connectionPool = new Queue<HttpConnection>();

        private readonly List<HttpConnection> activeConnections = new List<HttpConnection>();

        private readonly Dictionary<string, HttpServiceHandlerFactory> serviceFactories = new Dictionary<string, HttpServiceHandlerFactory>();
        internal readonly ServerConfig config;
        private Semaphore connectionLimit;

        public TimeSpan HttpTimeoutSpan { get; private set; }

        private Thread timerThread;

        public SpiderStudServer(ServerConfig config)
        {
            this.config = config;
            connectionLimit = new Semaphore(config.MaxConnections, config.MaxConnections);

            foreach (var endpoint in Endpoints)
            {
                var sock = SocketUtils.CreateListenSocket(endpoint.Address, SupportsDualStack);
                EndpointSockets.Add(sock);
            }
            HttpTimeoutSpan = new TimeSpan(config.HttpConnectionTimeout * TimeSpan.TicksPerMillisecond);
            timerThread = new Thread(ConnectionWatchThread);
        }

        public void Dispose()
        {
            connectionWatchRunning = false;
            foreach (var socket in EndpointSockets)
            {
                socket.Dispose();
            }
        }

        private bool ProcessAccept(SocketAsyncEventArgs e)
        {
            // This handles reading data from the new client socket
            OnClientConnect((HttpConnection)e.UserToken);

            // restart listening for this listener socket endpoint
            return e.ConnectSocket.AcceptAsync(e);
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            // need to repeat until we have triggered a delayed request
            while (!ProcessAccept(e)) ;
        }

        private void StartSocket(int socketIndex)
        {
            var socket = EndpointSockets[socketIndex];
            var endpoint = Endpoints[socketIndex];
            socket.Bind(endpoint);
            socket.Listen(config.RequestBacklogSize);

            if (socket.LocalEndPoint is IPEndPoint localEndpoint)
            {
                if (localEndpoint.Port != endpoint.Port)
                {
                    Logging.Warn("Socket not listening on correct port");
                }
                Logging.Info($"Server now listening at {localEndpoint.Address} (actual port {localEndpoint.Port})");
            }

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            HttpConnection connection;

            lock (connectionPool)
            {
                connection = connectionPool.Dequeue();
            }
            connection.InitConnection(endpoint);
            acceptEventArg.UserToken = connection;

            bool willTriggerCallback = socket.AcceptAsync(acceptEventArg);

            if (!willTriggerCallback)
            {
                // need to repeat until we have triggered a delayed request
                while (!ProcessAccept(acceptEventArg)) ;
            }
        }

        private void StartSockets()
        {
            for (int i = 0; i < EndpointSockets.Count; ++i)
            {
                try
                {
                    StartSocket(i);
                }
                catch (Exception e)
                {
                    TryRecoverSocket(i, e);
                }
            }
        }

        private void TryRecoverSocket(int socketIndex, Exception e)
        {
            Logging.Error("Listener socket is closed", e);
            if (SocketRestartAfterListenError)
            {
                Logging.Info("Listener socket restarting");
                try
                {
                    var endpoint = Endpoints[socketIndex];
                    EndpointSockets[socketIndex].Dispose();
                    EndpointSockets[socketIndex] = SocketUtils.CreateListenSocket(endpoint.Address, SupportsDualStack);
                    StartSocket(socketIndex);
                    Logging.Info("Listener socket restarted");
                }
                catch (Exception ex)
                {
                    Logging.Error("Listener could not be restarted", ex);
                }
            }
        }

        public IHttpServiceHandler? GetService(string resource)
        {
            if (serviceFactories.TryGetValue(resource, out var factory))
            {
                return factory?.Invoke();
            }
            return null;
        }

        public void WsService(string resource, WsServiceHandlerFactory clientHandlerFactory)
        {
            serviceFactories[resource] = () => new WebSocketHttpHandler(this, clientHandlerFactory);
        }

        public void HttpService(string resource, HttpServiceHandlerFactory clientHandlerFactory)
        {
            serviceFactories[resource] = clientHandlerFactory;
        }

        public void Start()
        {
            foreach (SecureIPEndpoint endpoint in Endpoints)
            {
                if (endpoint.Secure && !IsSecureSupported)
                {
                    throw new InvalidOperationException("TLS cannot be supported without a Certificate");
                }
            }

            StartSockets();
            timerThread.Start();

        }

        // Called after a client 
        internal void OnClientDisconnect(HttpConnection connection)
        {

            lock (connectionPool)
            {
                connectionPool.Enqueue(connection);
            }
        }

        private void OnClientConnect(HttpConnection connection)
        {
            Logging.Debug($"Client connected from {connection.ClientSocket.RemoteEndPoint}");

            lock (activeConnections)
            {
                activeConnections.Add(connection);
            }
            connection.StartReceiving();
        }

        private bool connectionWatchRunning = true;
        private void ConnectionWatchThread()
        {
            while (connectionWatchRunning)
            {
                lock (activeConnections)
                {
                    foreach (var connection in activeConnections)
                    {
                        connection.TimerTick();
                    }
                }
                Thread.Sleep(HttpTimeoutSpan);
            }
        }
    }
}
