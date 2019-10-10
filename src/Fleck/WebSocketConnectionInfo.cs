using System;
using System.Net;

namespace Fleck
{
    public class WebSocketConnectionInfo : IWebSocketConnectionInfo
    {
        public static WebSocketConnectionInfo Create(WebSocketHttpRequest request, IPAddress clientIp, int clientPort)
        {
            var info = new WebSocketConnectionInfo
            {
                Origin = request["Origin"] ?? request["Sec-WebSocket-Origin"],
                Host = request["Host"],
                SubProtocol = request["Sec-WebSocket-Protocol"],
                Path = request.Path,
                ClientIpAddress = clientIp,
                ClientPort = clientPort,
            };

            return info;
        }

        WebSocketConnectionInfo()
        {
            Id = Guid.NewGuid();
        }

        public string SubProtocol { get; private set; }
        public string Origin { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }
        public IPAddress ClientIpAddress { get; set; }
        public int ClientPort { get; set; }
        public Guid Id { get; set; }
    }
}
