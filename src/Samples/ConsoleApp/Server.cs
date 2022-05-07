using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;

namespace Fleck.Samples.ConsoleApp
{
    class ClientHandler : IWebSocketClientHandler
    {
        private IWebSocketConnection clientConnection;
        private WebSocketServer server;

        public void OnConfig(WebSocketServer wsServer, IWebSocketConnection connection)
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

        public static IWebSocketClientHandler Create()
        {
            return new ClientHandler();
        }
    }

    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(ClientHandler.Create);

            var input = Console.ReadLine();
            while (input != "exit")
            {
                // foreach (var socket in allSockets.ToList())
                // {
                //     socket.Send(input);
                // }
                input = Console.ReadLine();
            }

        }
    }
}
