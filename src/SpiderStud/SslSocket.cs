using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using SpiderStud.Helpers;

namespace SpiderStud
{

    public class SslSocket : ISocket
    {
        private Socket socket;
        private readonly CancellationTokenSource tokenSource;
        private readonly SslServerAuthenticationOptions authenticationOptions;
        private Stream stream;

        public IPEndPoint? LocalEndPoint => socket.LocalEndPoint as IPEndPoint;
        public IPEndPoint? RemoteEndPoint => socket.RemoteEndPoint as IPEndPoint;

        public IPAddress? RemoteIpAddress => socket.GetRemoteIPAddress();
        public int? RemotePort => socket.GetRemotePort();
        public IPAddress? LocalIpAddress => socket.GetLocalIPAddress();
        public int? LocalPort => socket.GetRemotePort();

        public bool Connected => socket.Connected;
        public int BytesAvailable => socket.Available;

        private readonly bool isManaged = false;
        private readonly IPAddress? managedAddress;
        private readonly bool managedIsDualStack;

        public bool NoDelay
        {
            get => socket.NoDelay;
            set => socket.NoDelay = value;
        }

        public SslSocket(IPAddress address, bool dualStack = true) : this(CreateSocket(address, dualStack))
        {
            isManaged = true;
            managedAddress = address;
            managedIsDualStack = dualStack;
        }

        public SslSocket(Socket socket)
        {
            this.socket = socket;
            tokenSource = new CancellationTokenSource();

            if (socket.Connected)
                stream = new NetworkStream(this.socket);
            authenticationOptions = new SslServerAuthenticationOptions();

            authenticationOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
            authenticationOptions.ClientCertificateRequired = false;
        }

        private static Socket CreateSocket(IPAddress address, bool dualStack)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.IP);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            if (dualStack)
            {
                if (!SpiderStudRuntime.IsRunningOnMono() && SpiderStudRuntime.IsRunningOnWindows())
                {
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                }
            }
            // The tcp keepalive default values on most systems
            // are huge (~7200s). Set them to something more reasonable.
            socket.SetKeepAlive();
            return socket;
        }

        public void Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols)
        {
            // Wrap network stream with ssl
            var ssl = new SslStream(stream, false);
            stream = ssl;

            ssl.AuthenticateAsServer(certificate, false, enabledSslProtocols, false);
        }

        public Task AuthenticateAsync(X509Certificate2 certificate, SslProtocols enabledSslProtocols)
        {
            // Wrap network stream with ssl
            var ssl = new SslStream(stream, false);
            stream = ssl;

            authenticationOptions.ServerCertificate = certificate;
            authenticationOptions.EnabledSslProtocols = enabledSslProtocols;

            return ssl.AuthenticateAsServerAsync(authenticationOptions, tokenSource.Token);
        }

        public void Listen(int backlog)
        {
            socket.Listen(backlog);
        }

        public void Bind(IPEndPoint endPoint)
        {
            socket.Bind(endPoint);
        }

        public int Read(Span<byte> data)
        {
            return stream.Read(data);
        }

        public ValueTask<int> ReadAsync(Memory<byte> data)
        {
            return stream.ReadAsync(data, tokenSource.Token);
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            stream.Write(data);
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data)
        {
            return stream.WriteAsync(data, tokenSource.Token);
        }

        public ISocket Accept()
        {
            Socket clientSocket = socket.Accept();
            var sockWrap = new SslSocket(clientSocket);
            return sockWrap;
        }

        public async ValueTask<SslSocket> AcceptAsync()
        {
            Socket clientSocket = await socket.AcceptAsync();
            var sockWrap = new SslSocket(clientSocket);
            return sockWrap;
        }

        public bool Restart()
        {
            if (!isManaged)
            {
                throw new NotSupportedException("Cannot restart a socket not managed by SslSocket");
            }

            if (managedAddress != null)
            {
                Dispose();
                socket = CreateSocket(managedAddress, managedIsDualStack);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            tokenSource.Cancel();
            if (stream != null) stream.Dispose();
            if (socket != null) socket.Dispose();
        }

        public void Close()
        {
            tokenSource.Cancel();
            if (stream != null) stream.Close();
            if (socket != null) socket.Close();
        }
    }
}
