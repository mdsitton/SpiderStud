using System;
using System.Buffers;

namespace Fleck
{
    public struct MemoryBuffer
    {
        public byte[] Data { get; private set; }
        public int Length { get; private set; }

        private readonly bool _fromPool;

        public MemoryBuffer(int minimumLength)
        {
            Data = ArrayPool<byte>.Shared.Rent(minimumLength);
            Length = Data.Length;
            _fromPool = true;
        }

        internal MemoryBuffer(byte[] data, int length, bool fromPool = true)
        {
            Data = data;
            Length = length;
            _fromPool = fromPool;
        }

        public void Dispose()
        {
            if (Data != null && _fromPool)
                ArrayPool<byte>.Shared.Return(Data);

            Data = null;
            Length = 0;
        }

        public MemoryBuffer DontDispose()
        {
            return new MemoryBuffer(Data, Length, false);
        }

        public MemoryBuffer Slice(int newLength)
        {
            if (newLength < 0 || newLength > Length)
                throw new ArgumentOutOfRangeException(nameof(newLength));

            return new MemoryBuffer(Data, newLength, _fromPool);
        }

        public static implicit operator Span<byte>(MemoryBuffer buffer)
        {
            return new Span<byte>(buffer.Data, 0, buffer.Length);
        }
    }
}
