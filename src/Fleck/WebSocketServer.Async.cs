using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using Fleck.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Fleck
{
    public partial class WebSocketServer
    {

        public async Task StartAsync()
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
                    throw new InvalidOperationException("Scheme cannot be 'wss' without a Certificate");
                }
            }
            await ListenForClientsAsync();
        }

        private async ValueTask ListenForClientsAsync()
        {
            while (clientConnectRunning)
            {
                try
                {
                    SslSocket clientSocket = await ListenerSocket.AcceptAsync();
                    await OnClientConnectAsync(clientSocket);
                }
                catch (ObjectDisposedException e)
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

        private async ValueTask OnClientConnectAsync(SslSocket clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            FleckLog.Debug($"Client connected from {clientSocket.RemoteIpAddress}:{clientSocket.RemotePort.ToString()}");

            var connection = new WebSocketConnection(clientSocket, clientHandlerFactory());

            if (IsSecure)
            {
                FleckLog.Debug("Authenticating Secure Connection");
                try
                {
                    await clientSocket.AuthenticateAsync(Certificate, EnabledSslProtocols);
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
                // connection.StartReceiving();
            }
        }
    }
}
