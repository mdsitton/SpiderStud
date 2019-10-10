using NUnit.Framework;
using System;
using System.Net;

namespace Fleck.Tests
{
    [TestFixture]
    public class WebSocketConnectionInfoTests
    {
        [Test]
        public void ShouldReadHeadersFromRequest()
        {
            const string origin = "http://blah.com/path/to/page";
            const string host = "blah.com";
            const string path = "/path/to/page";
            var clientIp = IPAddress.Parse("127.0.0.1");
            const int clientPort = 0;

            var request =
                new WebSocketHttpRequest
                    {
                        Headers =
                            {
                                {"Origin", origin},
                                {"Host", host},
                            },
                        Path = path
                    };
            var info = WebSocketConnectionInfo.Create(request, clientIp, clientPort);

            Assert.AreEqual(origin, info.Origin);
            Assert.AreEqual(host, info.Host);
            Assert.AreEqual(path, info.Path);
            Assert.AreEqual(clientIp, info.ClientIpAddress);
        }

        [Test]
        public void ShouldReadSecWebSocketOrigin()
        {
            const string origin = "http://example.com/myPath";
            var request =
                new WebSocketHttpRequest
                    {
                        Headers = { {"Sec-WebSocket-Origin", origin} }
                    };
            var info = WebSocketConnectionInfo.Create(request, IPAddress.None, 1);

            Assert.AreEqual(origin, info.Origin);
        }

        [Test]
        public void ShouldHaveId()
        {
            var request = new WebSocketHttpRequest();
            var info = WebSocketConnectionInfo.Create(request, IPAddress.None, 1);
            Assert.AreNotEqual(default(Guid), info.Id);
        }
    }
}
