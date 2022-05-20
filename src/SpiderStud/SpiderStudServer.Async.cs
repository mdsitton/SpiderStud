// using System;
// using System.Net;
// using System.Net.Sockets;
// using System.Security.Cryptography.X509Certificates;
// using System.Security.Authentication;
// using SpiderStud.Helpers;
// using System.Threading;
// using System.Threading.Tasks;

// namespace SpiderStud
// {
//     // public delegate IWebSocketClientHandlerAsync AsyncClientHandlerFactory();

//     public partial class SpiderStudServer
//     {
//         // AsyncClientHandlerFactory? clientHandlerAsyncFactory;

//         public async Task StartAsync(WsServiceHandlerFactory clientHandler)
//         {
//             var ipLocal = new IPEndPoint(locationIP, Port);
//             clientHandlerFactory = clientHandler;
//             StartSocket();
//             Logging.Info($"Server started at {Location} (actual port {Port})");
//             if (scheme == "wss" && Certificate == null)
//             {
//                 throw new InvalidOperationException("Scheme cannot be 'wss' without a Certificate");
//             }
//             await ListenForClientsAsync();
//         }

//         private async ValueTask ListenForClientsAsync()
//         {
//             while (clientConnectRunning)
//             {
//                 try
//                 {
//                     BcTlsSocket clientSocket = await ListenerSocket.AcceptAsync();
//                     await OnClientConnectAsync(clientSocket);
//                 }
//                 catch (ObjectDisposedException e)
//                 {
//                     TryRecoverSocket(e);
//                 }
//             }
//         }

//         private async ValueTask OnClientConnectAsync(BcTlsSocket clientSocket)
//         {
//             if (clientSocket == null) return; // socket closed

//             Logging.Debug($"Client connected from {clientSocket.RemoteIpAddress}:{clientSocket.RemotePort}");

//             if (clientHandlerFactory == null)
//             {
//                 throw new InvalidOperationException("Error server not properly initialized");
//             }
//             // TODO - implement async WebSocketConnection
//             var connection = new WebSocketConnection(clientSocket, this, clientHandlerFactory());

//             if (IsSecureSupported)
//             {
//                 if (Certificate == null)
//                 {
//                     throw new InvalidOperationException("Secure WebSocket must have certificates defined");
//                 }

//                 Logging.Debug("Authenticating Secure Connection");
//                 try
//                 {
//                     await clientSocket.AuthenticateAsync(Certificate, EnabledSslProtocols);
//                     // TODO - trigger receiving on client socket
//                     // connection.StartReceiving();
//                 }
//                 catch (Exception e)
//                 {
//                     Logging.Warn("Failed to Authenticate", e);
//                 }
//             }
//             else
//             {
//                 // connection.StartReceiving();
//             }
//         }
//     }
// }
