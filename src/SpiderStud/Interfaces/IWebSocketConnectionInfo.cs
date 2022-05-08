using System;
using System.Net;

namespace SpiderStud
{
    public interface IWebSocketConnectionInfo
    {
        Guid Id { get; }
        string SubProtocol { get; }
        string Origin { get; }
        string Host { get; }
        string Path { get; }
        IPAddress ClientIpAddress { get; }
        int ClientPort { get; }
    }
}
