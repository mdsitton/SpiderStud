using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using NSubstitute;
using Xunit;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.Tests
{
    public class WebSocketServerTests : IDisposable
    {
        private WebSocketServer _server;

        private X509Certificate2 cert;

        public WebSocketServerTests()
        {
            _server = new WebSocketServer("ws://0.0.0.0:8000");

            using var rsa = RSA.Create();
            var certRequest = new CertificateRequest("cn=fleckTest", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
        }

        [Fact]
        public void ShouldStart()
        {
            var socketMock = Substitute.For<ISocket>();
            var ipLocal = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8000);
            socketMock.LocalEndPoint.Returns(ipLocal);
            socketMock.Accept().Returns(Substitute.For<ISocket>());
            _server.ListenerSocket = socketMock;
            _server.Start(() => Substitute.For<IWebSocketClientHandler>());


            socketMock.Received().Bind(ipLocal);
            socketMock.Received().Accept();
        }

        [Fact]
        public void ShouldFailToParseIPAddressOfLocation()
        {
            Assert.Throws<FormatException>(() =>
            {
                new WebSocketServer("ws://localhost:8000");
            });
        }

        [Fact]
        public void ShouldBeSecureWithWssAndCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            server.Certificate = cert;
            Assert.True(server.IsSecure);
        }

        [Fact]
        public void ShouldDefaultToNoneWithWssAndCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            server.Certificate = cert;
            Assert.Equal(SslProtocols.None, server.EnabledSslProtocols);
        }

        [Fact]
        public void ShouldNotBeSecureWithWssAndNoCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            Assert.False(server.IsSecure);
        }

        [Fact]
        public void ShouldNotBeSecureWithoutWssAndCertificate()
        {
            var server = new WebSocketServer("ws://0.0.0.0:8000");
            server.Certificate = cert;
            Assert.False(server.IsSecure);
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
