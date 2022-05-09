using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using SpiderStud.Helpers;
using System.Threading;
using System.Collections.Generic;

namespace SpiderStud
{
    public delegate IWebSocketServiceHandler ClientHandlerFactory();

    public partial class WebSocketServer : IDisposable
    {
        private readonly string scheme;
        private readonly IPAddress locationIP;
        private ClientHandlerFactory? clientHandlerFactory;
        private Thread? clientConnectionThread = null;
        private Thread? clientReceiveThread = null;

        public ISocket ListenerSocket { get; set; }
        public string Location { get; }
        public bool SupportDualStack { get; }
        public int Port { get; private set; }
        public X509Certificate2? Certificate { get; set; }
        public SslProtocols EnabledSslProtocols { get; set; }
        public bool RestartAfterListenError { get; set; }

        public bool IsSecure => scheme == "wss" && Certificate != null;

        readonly List<WebSocketConnection> activeConnections = new List<WebSocketConnection>();

        public WebSocketServer(string location, bool supportDualStack = true)
        {
            var uri = new Uri(location);
            Port = uri.Port;
            locationIP = ParseIPAddress(uri);
            scheme = uri.Scheme;

            Location = location;
            SupportDualStack = supportDualStack;
            ListenerSocket = new SslSocket(locationIP);
        }

        public WebSocketServer(string location, ISocket socket, bool supportDualStack = true)
        {
            Location = location;
            SupportDualStack = supportDualStack;
            ListenerSocket = socket;

            var uri = new Uri(location);
            Port = uri.Port;
            locationIP = ParseIPAddress(uri);
            scheme = uri.Scheme;
        }

        public void Dispose()
        {
            clientReceiveRunning = false;
            clientConnectRunning = false;
            ListenerSocket.Dispose();
        }

        private IPAddress ParseIPAddress(Uri uri)
        {
            var ipStr = uri.Host;

            if (ipStr == "0.0.0.0")
                return IPAddress.Any;

            if (ipStr == "[0000:0000:0000:0000:0000:0000:0000:0000]")
                return IPAddress.IPv6Any;

            try
            {
                return IPAddress.Parse(ipStr);
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to parse the IP address part of the location. Please make sure you specify a valid IP address. Use 0.0.0.0 or [::] to listen on all interfaces.", ex);
            }
        }

        public void StartSocket()
        {
            var ipLocal = new IPEndPoint(locationIP, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);
            Port = ((IPEndPoint)ListenerSocket.LocalEndPoint).Port;
        }

        private void TryRecoverSocket(Exception e)
        {
            SpiderStudLog.Error("Listener socket is closed", e);
            if (RestartAfterListenError)
            {
                SpiderStudLog.Info("Listener socket restarting");
                try
                {
                    if (ListenerSocket.Restart())
                    {
                        StartSocket();
                        SpiderStudLog.Info("Listener socket restarted");
                    }
                    else
                    {
                        SpiderStudLog.Error("Listener socket failed to restart");
                        throw e;
                    }
                }
                catch (Exception ex)
                {
                    SpiderStudLog.Error("Listener could not be restarted", ex);
                }
            }
        }

        // public void Start<T>() where T : IWebSocketClientHandler
        public void Start(ClientHandlerFactory clientHandlerFactory)
        {
            this.clientHandlerFactory = clientHandlerFactory;
            StartSocket();
            SpiderStudLog.Info($"Server started at {Location} (actual port {Port})");

            if (scheme == "wss" && Certificate == null)
            {
                throw new InvalidOperationException("Scheme cannot be 'wss' without a Certificate");
            }

            if (clientConnectionThread == null)
            {
                clientConnectionThread = new Thread(ClientConnectionThread);
                clientConnectionThread.Start();
            }

            if (clientReceiveThread == null)
            {
                clientReceiveThread = new Thread(ClientReceiveThread);
                clientReceiveThread.Start();
            }
        }

        private void OnClientConnect(ISocket clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            SpiderStudLog.Debug($"Client connected from {clientSocket.RemoteIpAddress}:{clientSocket.RemotePort}");

            if (clientHandlerFactory == null)
            {
                throw new InvalidOperationException("Error server not properly initialized");
            }

            var connection = new WebSocketConnection(clientSocket, this, clientHandlerFactory());

            lock (activeConnections)
            {
                activeConnections.Add(connection);
            }

            if (IsSecure)
            {
                if (Certificate == null)
                {
                    throw new InvalidOperationException("Secure WebSocket must have certificates defined");
                }

                SpiderStudLog.Debug("Authenticating Secure Connection");
                try
                {
                    clientSocket.Authenticate(Certificate, EnabledSslProtocols);
                    // TODO - trigger receiving on client socket
                    // connection.StartReceiving();
                }
                catch (Exception e)
                {
                    SpiderStudLog.Warn("Failed to Authenticate", e);
                }
            }
            else
            {
                // TODO - trigger receiving on client socket
                // connection.StartReceiving();
            }
        }

        private bool clientReceiveRunning = true;
        private void ClientReceiveThread()
        {
            while (clientReceiveRunning)
            {
                bool foundSocketData = false;
                lock (activeConnections)
                {
                    foreach (var connection in activeConnections)
                    {
                        if (connection.Socket.BytesAvailable > 0)
                        {
                            foundSocketData = true;
                            connection.Update();
                        }
                    }
                }
                if (!foundSocketData)
                {
                    Thread.Sleep(5);
                }
            }
        }

        private bool clientConnectRunning = true;
        private void ClientConnectionThread()
        {
            while (clientConnectRunning)
            {
                try
                {
                    var clientSocket = ListenerSocket.Accept();
                    OnClientConnect(clientSocket);
                }
                catch (Exception e)
                {
                    TryRecoverSocket(e);
                }
            }
        }

    }
}
