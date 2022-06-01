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
using System.Collections.Concurrent;

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
        public bool SupportsDualStack => config.DualStackSupport;
        public IReadOnlyList<SecureIPEndpoint> Endpoints => config.Endpoints;
        public bool SocketRestartAfterListenError => config.SocketRestartAfterListenError;
        public bool IsSecureSupported => config.Certificate != null;

        // We use a Queue of connections so that reused sockets have time to linger and fully close
        private readonly List<HttpConnection> connections = new List<HttpConnection>();

        private readonly ConcurrentQueue<HttpConnection> freeConnections = new ConcurrentQueue<HttpConnection>();

        private readonly Dictionary<string, HttpServiceHandlerFactory> serviceFactories = new Dictionary<string, HttpServiceHandlerFactory>();
        internal readonly ServerConfig config;
        private Semaphore connectionLimit;

        public TimeSpan HttpTimeoutSpan { get; private set; }

        private Thread timerThread;

        public bool Running => connectionWatchRunning;

        public SpiderStudServer(ServerConfig config)
        {
            this.config = config;
            connectionLimit = new Semaphore(config.MaxConnections, config.MaxConnections);

            foreach (var endpoint in Endpoints)
            {
                endpoint.socketInstance = SocketUtils.CreateListenSocket(endpoint.Address, SupportsDualStack);
            }
            HttpTimeoutSpan = new TimeSpan(config.HttpConnectionTimeout * TimeSpan.TicksPerMillisecond);
            timerThread = new Thread(ConnectionWatchThread);
            for (int i = 0; i < config.MaxConnections; ++i)
            {
                connections.Add(new HttpConnection(this));
            }
        }

        public void Dispose()
        {
            connectionWatchRunning = false;
            foreach (var endpoint in Endpoints)
            {
                endpoint.socketInstance?.Dispose();
            }
        }

        private bool ProcessAccept(SocketAsyncEventArgs e)
        {
            var connection = (HttpConnection)e.UserToken;
            // This handles reading data from the new client socket
            OnClientConnect(connection);

            if (connection?.ConnectedEndpoint == null)
                return true;

            // restart listening for this listener socket endpoint
            // TODO - ConnectSocket will not have the correct socket that we need
            // to restart listening, need to figure out a different way to do this
            return DoAccept(connection.ConnectedEndpoint, e);
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            // need to repeat until we have triggered a delayed request
            while (!ProcessAccept(e)) ;
        }

        private void StartSocket(SecureIPEndpoint endpoint)
        {
            var socket = endpoint.socketInstance;
            if (socket == null)
                return;

            socket.Bind(endpoint);
            socket.Listen(config.RequestBacklogSize);

            if (socket.LocalEndPoint is IPEndPoint localEndpoint)
            {
                if (localEndpoint.Port != endpoint.Port)
                {
                    Logging.Warn("Socket not listening on correct port");
                }
                Logging.Info<IPAddress, int>("Server now listening at {0} (actual port {1})", localEndpoint.Address, localEndpoint.Port);
            }

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            if (!DoAccept(endpoint, acceptEventArg))
            {
                // need to repeat until we have triggered a delayed request
                while (!ProcessAccept(acceptEventArg)) ;
            }
        }

        private bool DoAccept(SecureIPEndpoint endpoint, SocketAsyncEventArgs acceptEventArg)
        {
            HttpConnection? connection = FindUnusedConnection();

            while (connection == null)
            {
                Thread.Sleep(5);
                connection = FindUnusedConnection();
            }
            connection.InitConnection(endpoint);
            acceptEventArg.UserToken = connection;
            acceptEventArg.AcceptSocket = connection.ClientSocket;

            var rtn = endpoint.socketInstance!.AcceptAsync(acceptEventArg);

            return rtn;
        }

        private void StartSockets()
        {
            foreach (var endpoint in Endpoints)
            {
                try
                {
                    StartSocket(endpoint);
                }
                catch (Exception e)
                {
                    TryRecoverSocket(endpoint, e);
                }
            }
        }

        private void TryRecoverSocket(SecureIPEndpoint endpoint, Exception e)
        {
            Logging.Exception(e, "Listener socket is closed");
            if (SocketRestartAfterListenError)
            {
                Logging.Info("Listener socket restarting");
                try
                {
                    endpoint.socketInstance?.Dispose();
                    endpoint.socketInstance = SocketUtils.CreateListenSocket(endpoint.Address, SupportsDualStack);
                    StartSocket(endpoint);
                    Logging.Info("Listener socket restarted");
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex, "Listener could not be restarted");
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

        internal HttpConnection? FindUnusedConnection()
        {
            if (freeConnections.TryDequeue(out HttpConnection connection) && connection.ClientSocket.Connected != true)
            {
                Logging.Debug("Reused free connection");
                connection.isClaimed = true;
                return connection;
            }
            foreach (var conn in connections)
            {
                if (conn.ClientSocket.Connected != true && !conn.isClaimed)
                {
                    Logging.Debug("Searched for available connection");
                    conn.isClaimed = true;
                    return conn;
                }
            }

            return null;
        }

        // Called after a client 
        internal void OnClientDisconnect(HttpConnection connection)
        {
            connection.isClaimed = false;
            freeConnections.Enqueue(connection);
        }

        private void OnClientConnect(HttpConnection connection)
        {
            Logging.Debug("Client connected from {0}", connection.ClientSocket.RemoteEndPoint);
            connection.StartReceiving();
        }

        private bool connectionWatchRunning = true;
        private void ConnectionWatchThread()
        {
            while (connectionWatchRunning)
            {
                foreach (var connection in connections)
                {
                    if (connection.IsAvailable)
                    {
                        connection.TimerTick();
                    }
                }
                Thread.Sleep(HttpTimeoutSpan);
            }
        }
    }
}
