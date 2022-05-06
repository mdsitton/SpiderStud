using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using Fleck.Helpers;
using System.Threading;

namespace Fleck
{
    public delegate IWebSocketClientHandler ClientHandlerFactory();

    public partial class WebSocketServer
    {
        private readonly string scheme;
        private readonly IPAddress locationIP;
        private readonly ClientHandlerFactory clientHandlerFactory;
        private Thread? clientListenerThread = null;

        public SslSocket ListenerSocket { get; set; }
        public string Location { get; }
        public bool SupportDualStack { get; }
        public int Port { get; private set; }
        public X509Certificate2? Certificate { get; set; }
        public SslProtocols EnabledSslProtocols { get; set; }
        public bool RestartAfterListenError { get; set; }

        public bool IsSecure => scheme == "wss" && Certificate != null;

        public WebSocketServer(string location, ClientHandlerFactory clientHandlerFactory, bool supportDualStack = true)
        {
            var uri = new Uri(location);

            Port = uri.Port;
            Location = location;
            this.clientHandlerFactory = clientHandlerFactory;
            SupportDualStack = supportDualStack;

            locationIP = ParseIPAddress(uri);
            scheme = uri.Scheme;
            var socket = new Socket(locationIP.AddressFamily, SocketType.Stream, ProtocolType.IP);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            if (SupportDualStack)
            {
                if (!FleckRuntime.IsRunningOnMono() && FleckRuntime.IsRunningOnWindows())
                {
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                }
            }

            ListenerSocket = new SslSocket(socket);
        }

        public void Dispose()
        {
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

        public void Start()
        {
            var ipLocal = new IPEndPoint(locationIP, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);
            Port = ((IPEndPoint)ListenerSocket.LocalEndPoint).Port;
            FleckLog.Info($"Server started at {Location} (actual port {Port})");
            if (scheme == "wss")
            {
                if (Certificate == null)
                {
                    FleckLog.Error("Scheme cannot be 'wss' without a Certificate");
                    return;
                }
            }
            clientListenerThread = new Thread(ClientListenerThread);
            clientListenerThread.Start();
        }

        private bool clientConnectRunning = false;
        private void ClientListenerThread()
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
                    FleckLog.Error("Listener socket is closed", e);
                    if (RestartAfterListenError)
                    {
                        FleckLog.Info("Listener socket restarting");
                        try
                        {
                            ListenerSocket.Dispose();
                            var socket = new Socket(locationIP.AddressFamily, SocketType.Stream, ProtocolType.IP);
                            ListenerSocket = new SslSocket(socket);
                            Start();
                            FleckLog.Info("Listener socket restarted");
                        }
                        catch (Exception ex)
                        {
                            FleckLog.Error("Listener could not be restarted", ex);
                        }
                    }
                }
            }
        }

        private void OnClientConnect(SslSocket clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            FleckLog.Debug($"Client connected from {clientSocket.RemoteIpAddress}:{clientSocket.RemotePort}");

            var connection = new WebSocketConnection(clientSocket, clientHandlerFactory());

            if (IsSecure)
            {
                if (Certificate == null)
                {
                    throw new InvalidOperationException("Secure WebSocket must have certificates defined");
                }

                FleckLog.Debug("Authenticating Secure Connection");
                try
                {
                    clientSocket.Authenticate(Certificate, EnabledSslProtocols);
                    // TODO - trigger receiving on client socket
                    // connection.StartReceiving();
                }
                catch (Exception e)
                {
                    FleckLog.Warn("Failed to Authenticate", e);
                }
            }
            else
            {
                // TODO - trigger receiving on client socket
                // connection.StartReceiving();
            }
        }

    }
}
