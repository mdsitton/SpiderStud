using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiderStud.Http;

namespace SpiderStud
{
    public class WebSocketHttpHandler : IHttpServiceHandler
    {
        private WsServiceHandlerFactory WebSocketClientFactory;
        private List<WebSocketConnection> activeConnections = new List<WebSocketConnection>();
        private SpiderStudServer server;

        public WebSocketHttpHandler(WsServiceHandlerFactory wsFactory)
        {
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

        public void OnStart(SpiderStudServer server)
        {
            this.server = server;
        }
    }
}