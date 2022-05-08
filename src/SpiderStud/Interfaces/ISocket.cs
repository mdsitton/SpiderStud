using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SpiderStud
{
    public interface ISocket
    {
        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }
        IPAddress? RemoteIpAddress { get; }
        int? RemotePort { get; }
        IPAddress? LocalIpAddress { get; }
        int? LocalPort { get; }
        bool Connected { get; }
        int BytesAvailable { get; }
        bool NoDelay { get; set; }

        ISocket Accept();
        ValueTask<SslSocket> AcceptAsync();
        void Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols);
        Task AuthenticateAsync(X509Certificate2 certificate, SslProtocols enabledSslProtocols);
        void Bind(EndPoint endPoint);
        void Close();
        void Dispose();
        bool Restart();
        void Listen(int backlog);
        int Read(Span<byte> data);
        ValueTask<int> ReadAsync(Memory<byte> data);
        void Write(ReadOnlySpan<byte> data);
        ValueTask WriteAsync(ReadOnlyMemory<byte> data);
    }
}