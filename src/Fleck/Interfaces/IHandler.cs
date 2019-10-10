using System;

namespace Fleck
{
    public interface IHandler : IDisposable
    {
        void Receive(Span<byte> newData);
        MemoryBuffer CreateHandshake();
        MemoryBuffer FrameText(string text);
        MemoryBuffer FrameBinary(byte[] bytes);
        MemoryBuffer FramePing(byte[] bytes);
        MemoryBuffer FramePong(byte[] bytes);
        MemoryBuffer FrameClose(ushort code);
    }
}

