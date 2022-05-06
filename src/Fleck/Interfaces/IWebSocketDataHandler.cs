using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fleck
{
    public interface IWebSocketClientHandler
    {
        void OnError(Exception e);
        void OnOpen();
        void OnClose();
        void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data);
    }
}