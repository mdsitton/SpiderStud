using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud
{
    public interface IWebSocketServiceHandler : IDisposable
    {
        void OnConfig(SpiderStudServer server, IWebSocketConnection connection);
        void OnOpen();
        void OnClose();
        void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data);
    }
}