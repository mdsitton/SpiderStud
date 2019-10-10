using System;

namespace Fleck
{
    public interface IHandler : IDisposable
    {
        void Receive(Span<byte> newData);
        MemoryBuffer CreateHandshake();
        MemoryBuffer FrameText(string text);
        MemoryBuffer FrameBinary(ArraySegment<byte> bytes);
        MemoryBuffer FramePing(ArraySegment<byte> bytes);
        MemoryBuffer FramePong(ArraySegment<byte> bytes);
        MemoryBuffer FrameClose(ushort code);
    }
}

