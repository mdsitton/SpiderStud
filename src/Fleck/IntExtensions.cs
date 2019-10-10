using System;
using System.Buffers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fleck.Tests")]

namespace Fleck
{
    // assuming little endian CPU because almost nothing is big endian
    internal static unsafe class IntExtensions
    {
        public static IMemoryOwner<byte> ToBigEndianBytes<T>(this int source)
        {
            if (typeof(T) == typeof(ushort))
                return CopyToMemory((ushort)source);

            if (typeof(T) == typeof(ulong))
                return CopyToMemory((ulong)source);

            throw new InvalidCastException("Cannot be cast to T");
        }

        public static int ToLittleEndianInt(this Span<byte> source)
        {
            if (source.Length == 2)
                return CopyFromMemory<ushort>(source);

            if (source.Length == 8)
                return (int)CopyFromMemory<ulong>(source);

            throw new ArgumentException("Unsupported Size");
        }

        private static IMemoryOwner<byte> CopyToMemory<T>(T value) where T : unmanaged
        {
            var valueSpan = new Span<byte>(&value, sizeof(T));
            valueSpan.Reverse();

            var memory = MemoryPool<byte>.Shared.Rent(sizeof(T));
            valueSpan.CopyTo(memory.Memory.Span);
            return memory;
        }

        private static T CopyFromMemory<T>(Span<byte> memory) where T : unmanaged
        {
            if (memory.Length != sizeof(T))
                throw new ArgumentException($"Cannot copy from memory: expected {sizeof(T)} bytes, got {memory.Length}");

            Span<byte> copy = stackalloc byte[memory.Length];
            memory.CopyTo(copy);

            copy.Reverse();
            fixed (byte* ptr = &copy[0])
            {
                return *(T*)ptr;
            }
        }
    }
}
