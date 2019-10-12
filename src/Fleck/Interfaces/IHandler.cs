using System;

namespace Fleck
{
    public interface IHandler : IDisposable
    {
        void Receive(Span<byte> newData);
        MemoryBuffer CreateHandshake();
        MemoryBuffer FrameText(string text);
        MemoryBuffer FrameBinary(MemoryBuffer bytes);
        MemoryBuffer FramePing(MemoryBuffer bytes);
        MemoryBuffer FramePong(MemoryBuffer bytes);
        MemoryBuffer FrameClose(ushort code);
    }
}

