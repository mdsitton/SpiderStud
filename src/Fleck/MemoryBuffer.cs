using System;
using System.Buffers;

namespace Fleck
{
    public readonly struct MemoryBuffer : IDisposable
    {
        public byte[] Data { get; }
        public int Length { get; }

        private readonly bool _fromPool;

        public MemoryBuffer(byte[] data, int length, bool fromPool = true)
        {
            Data = data;
            Length = length;
            _fromPool = fromPool;
        }

        public MemoryBuffer(byte[] data)
        {
            Data = data;
            Length = data?.Length ?? 0;
            _fromPool = false;
        }

        public void Dispose()
        {
            if (_fromPool)
                ArrayPool<byte>.Shared.Return(Data);
        }
    }
}
