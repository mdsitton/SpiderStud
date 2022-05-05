using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fleck
{
    public interface ISocket
    {
        bool Connected { get; }
        int BytesAvailable { get; set; }

        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }

        IPAddress? RemoteIpAddress { get; }
        int? RemotePort { get; }
        bool NoDelay { get; set; }

        ISocket Accept();
        ValueTask<ISocket> AcceptAsync();

        void Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols);
        Task AuthenticateAsync(X509Certificate2 certificate, SslProtocols enabledSslProtocols);

        int Read(Span<byte> data);
        ValueTask<int> ReadAsync(Memory<byte> data);

        void Write(ReadOnlySpan<byte> data);
        ValueTask WriteAsync(ReadOnlyMemory<byte> data);

        void Dispose();
        void Close();

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);
    }
}
