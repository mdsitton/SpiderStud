using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud
{
    public interface IWebSocketClientHandler
    {
        void OnConfig(WebSocketServer server, IWebSocketConnection connection);
        void OnError(Exception e);
        void OnOpen();
        void OnClose();
        void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data);
    }
}