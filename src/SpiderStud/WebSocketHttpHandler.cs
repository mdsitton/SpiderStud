using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiderStud.Http;

namespace SpiderStud
{
    public class WebSocketHttpHandler : IHttpServiceHandler
    {
        private readonly WsServiceHandlerFactory WebSocketClientFactory;
        private readonly List<WebSocketConnection> activeConnections = new List<WebSocketConnection>();
        private readonly SpiderStudServer server;

        public WebSocketHttpHandler(SpiderStudServer server, WsServiceHandlerFactory wsFactory)
        {
            this.server = server;
            WebSocketClientFactory = wsFactory;
        }

        public void Dispose()
        {
        }

        public void OnRequest(HttpRequest request)
        {
            // Validate handshake

            // Send back response

            // create new WebSocketConnection
        }

        public void OnStart()
        {
        }
    }
}