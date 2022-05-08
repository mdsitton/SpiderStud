using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud
{
    public interface IWebSocketClientHandlerAsync
    {
        Task OnConfig(WebSocketServer server, IWebSocketConnection connection);
        Task OnError(Exception e);
        Task OnOpen();
        Task OnClose();
        Task OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data);
    }
}