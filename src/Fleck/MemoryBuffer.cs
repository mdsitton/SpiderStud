using System;
using System.Buffers;

namespace Fleck
{
    public readonly struct MemoryBuffer : IDisposable
    {
        public byte[] Data { get; }
        public int Length { get; }

        public MemoryBuffer(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }

        public MemoryBuffer(byte[] data)
        {
            Data = data;
            Length = data?.Length ?? 0;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Data);
        }
    }
}
