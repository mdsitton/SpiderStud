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
    // public delegate IWebSocketClientHandlerAsync AsyncClientHandlerFactory();

    public partial class WebSocketServer
    {
        // AsyncClientHandlerFactory? clientHandlerAsyncFactory;

        public async Task StartAsync(ClientHandlerFactory clientHandler)
        {
            var ipLocal = new IPEndPoint(locationIP, Port);
            clientHandlerFactory = clientHandler;
            StartSocket();
            FleckLog.Info($"Server started at {Location} (actual port {Port})");
            if (scheme == "wss" && Certificate == null)
            {
                throw new InvalidOperationException("Scheme cannot be 'wss' without a Certificate");
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
                    TryRecoverSocket(e);
                }
            }
        }

        private async ValueTask OnClientConnectAsync(SslSocket clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            FleckLog.Debug($"Client connected from {clientSocket.RemoteIpAddress}:{clientSocket.RemotePort}");

            if (clientHandlerFactory == null)
            {
                throw new InvalidOperationException("Error server not properly initialized");
            }
            // TODO - implement async WebSocketConnection
            var connection = new WebSocketConnection(clientSocket, this, clientHandlerFactory());

            if (IsSecure)
            {
                if (Certificate == null)
                {
                    throw new InvalidOperationException("Secure WebSocket must have certificates defined");
                }

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
