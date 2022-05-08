using System;
using System.Net;

namespace SpiderStud
{
    public class WebSocketConnectionInfo : IWebSocketConnectionInfo
    {
        public static WebSocketConnectionInfo Create(WebSocketHttpRequest request, IPAddress clientIp, int clientPort)
        {
            var info = new WebSocketConnectionInfo
            {
                Origin = request.Headers["Origin"],
                Host = request.Headers["Host"],
                SubProtocol = request.Headers["Sec-WebSocket-Protocol"],
                ProtocolExtensions = request.Headers["Sec-WebSocket-Extensions"],
                Key = request.Headers["Sec-WebSocket-Key"],
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

        public string ProtocolExtensions { get; private set; }
        public string SubProtocol { get; private set; }
        public string Origin { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }
        public string Key { get; private set; }
        public IPAddress ClientIpAddress { get; set; }
        public int ClientPort { get; set; }
        public Guid Id { get; set; }
    }
}
