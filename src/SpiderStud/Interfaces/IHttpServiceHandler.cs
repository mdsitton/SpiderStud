using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud.Http
{
    public interface IHttpServiceHandler
    {
        void OnStart(WebSocketServer server, IWebSocketConnection connection);
        void OnError(Exception e);
        HttpResponse OnRequest(HttpRequest request);
        void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data);
    }
}