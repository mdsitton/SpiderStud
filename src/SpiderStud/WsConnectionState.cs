using System;
using System.Net;
using SpiderStud.Http;

namespace SpiderStud
{
    public readonly struct WsConnectionState
    {
        public WsConnectionState(
            HttpRequest request, IPAddress clientIp,
            int clientPort, string negotiatedSubProtocol)
        {
            Origin = request.Headers["Origin"];
            Host = request.Headers["Host"];
            SubProtocol = request.Headers["Sec-WebSocket-Protocol"];
            ProtocolExtensions = request.Headers["Sec-WebSocket-Extensions"];
            Key = request.Headers["Sec-WebSocket-Key"];
            Path = request.Path;
            ClientIpAddress = clientIp;
            ClientPort = clientPort;
            NegotiatedSubProtocol = negotiatedSubProtocol;
            Id = Guid.NewGuid();
        }

        public string ProtocolExtensions { get; }
        public string SubProtocol { get; }
        public string NegotiatedSubProtocol { get; }
        public string Origin { get; }
        public string Host { get; }
        public string Path { get; }
        public string Key { get; }
        public IPAddress ClientIpAddress { get; }
        public int ClientPort { get; }
        public Guid Id { get; }
    }
}
