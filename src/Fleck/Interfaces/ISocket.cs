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
        IPAddress RemoteIpAddress { get; }
        int RemotePort { get; }
        Stream Stream { get; }
        bool NoDelay { get; set; }
        EndPoint LocalEndPoint { get; }

        Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error);
        Task Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols, Action callback, Action<Exception> error);

        void Dispose();
        void Close();

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);
    }
}
