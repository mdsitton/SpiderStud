using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using SpiderStud.Http;

namespace SpiderStud.Samples.ConsoleApp
{
    // A seperate instance of the service handler is used for each client connection
    class ClientServiceHandler : IWebSocketServiceHandler
    {
        private IWebSocketConnection clientConnection;
        private SpiderStudServer server;

        public void OnConfig(SpiderStudServer wsServer, IWebSocketConnection connection)
        {
            clientConnection = connection;
            server = wsServer;
        }

        public void OnError(Exception e)
        {
        }

        public void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data)
        {
            if (type == FrameType.Text)
            {
                string messageText = Encoding.UTF8.GetString(data);
                if (messageText == "close")
                {
                    Console.WriteLine("Closing socket!");
                    clientConnection.Close();
                    return;
                }

                Console.WriteLine(messageText);
                // allSockets.ToList().ForEach(s => s.Send("Echo: " + messageText));
            }
        }

        public void OnOpen()
        {
            Console.WriteLine("Open!");
        }

        public void OnClose()
        {
            Console.WriteLine("Close!");
        }

        public static IWebSocketServiceHandler Create()
        {
            return new ClientServiceHandler();
        }

        public void Dispose()
        {
            // Close any resources required
        }
    }

    class Server
    {
        static void Main()
        {
            Logging.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var config = new ServerConfig()
            {
                SocketRestartAfterListenError = true,
                DualStackSupport = true,
                MaxConnections = 256,
                HttpConnectionTimeout = 60000,
                WsConnectionTimeout = 60000,

                // TODO - Implement utility function to take a URI string and convert to SecureIpEndpoint:
                // ws://0.0.0.0:8181 or wss://192.168.1.10:4134
                // http://0.0.0.0:8080 or https://192.168.1.10:4134
                Endpoints = new List<SecureIPEndpoint> { new(System.Net.IPAddress.Any, 8181, false) }
            };
            var server = new SpiderStudServer(config);
            server.HttpService("/", () => new DefaultHttpServiceHandler());
            server.Start();

            while (server.Running)
            {
                Thread.Sleep(1000);
            }

            // var input = Console.ReadLine();
            // while (input != "exit")
            // {
            //     // foreach (var socket in allSockets.ToList())
            //     // {
            //     //     socket.Send(input);
            //     // }
            //     input = Console.ReadLine();
            // }

        }
    }
}
