// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on Andrew Arnott's Nerdbank.Streams PrefixingBufferWriter<T> type
// from https://github.com/AArnott/Nerdbank.Streams/blob/main/src/Nerdbank.Streams/PrefixingBufferWriter%601.cs

using System;
using System.Buffers;

namespace SpiderStud
{
    /// <summary>
    /// An <see cref="IBufferWriter{T}"/> that reserves some fixed size for a header.
    /// </summary>
    /// <typeparam name="T">The type of element written by this writer.</typeparam>
    /// <remarks>
    /// This type is used for inserting the length of list in the header when the length is not known beforehand.
    /// It is optimized to minimize or avoid copying.
    /// </remarks>
    public class PrefixingBufferWriter<T> : IBufferWriter<T>
    {
        /// <summary>
        /// The value to use in place of <see cref="payloadSizeHint"/> when it is 0.
        /// </summary>
        /// <remarks>
        /// We choose ~4K, since 4K is the default size for buffers in a lot of corefx libraries.
        /// We choose 4K - 4 specifically because length prefixing is so often for an <see cref="int"/> value,
        /// and if we ask for 1 byte more than 4K, memory pools tend to give us 8K.
        /// </remarks>
        private const int PayloadSizeGuess = 4092;

        /// <summary>
        /// The underlying buffer writer.
        /// </summary>
        private readonly IBufferWriter<T> innerWriter;

        /// <summary>
        /// The length of the prefix to reserve space for.
        /// </summary>
        private readonly int expectedPrefixSize;

        /// <summary>
        /// The minimum space to reserve for the payload when first asked for a buffer.
        /// </summary>
        /// <remarks>
        /// This, added to <see cref="expectedPrefixSize"/>, makes up the minimum size to request from <see cref="innerWriter"/>
        /// to minimize the chance that we'll need to copy buffers from <see cref="excessSequence"/> to <see cref="innerWriter"/>.
        /// </remarks>
        private readonly int payloadSizeHint;

        /// <summary>
        /// The pool to use when initializing <see cref="excessSequence"/>.
        /// </summary>
        private readonly MemoryPool<T> memoryPool;

        /// <summary>
        /// The buffer writer to use for all buffers after the original one obtained from <see cref="innerWriter"/>.
        /// </summary>
        private SequenceWriter<T>? excessSequence;

        /// <summary>
        /// The buffer from <see cref="innerWriter"/> reserved for the fixed-length prefix.
        /// </summary>
        private Memory<T> prefixMemory;

        /// <summary>
        /// The memory being actively written to, which may have come from <see cref="innerWriter"/> or <see cref="excessSequence"/>.
        /// </summary>
        private Memory<T> realMemory;

        /// <summary>
        /// The number of elements written to the original buffer obtained from <see cref="innerWriter"/>.
        /// </summary>
        private int advanced;

        /// <summary>
        /// A value indicating whether we're using <see cref="excessSequence"/> in the current state.
        /// </summary>
        private bool usingExcessMemory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefixingBufferWriter{T}"/> class.
        /// </summary>
        /// <param name="innerWriter">The underlying writer that should ultimately receive the prefix and payload.</param>
        /// <param name="prefixSize">The length of the header to reserve space for. Must be a positive number.</param>
        /// <param name="payloadSizeHint">A hint at the expected max size of the payload. The real size may be more or less than this, but additional copying is avoided if it does not exceed this amount. If 0, a reasonable guess is made.</param>
        public PrefixingBufferWriter(IBufferWriter<T> innerWriter, int prefixSize, int payloadSizeHint = 0)
        {
            if (prefixSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixSize));
            }

            this.innerWriter = innerWriter ?? throw new ArgumentNullException(nameof(innerWriter));
            this.expectedPrefixSize = prefixSize;
            this.payloadSizeHint = payloadSizeHint;
            this.memoryPool = MemoryPool<T>.Shared;
        }

        /// <summary>
        /// Gets the sum of all values passed to <see cref="Advance(int)"/> since
        /// the last call to <see cref="Commit"/>.
        /// </summary>
        public long Length => (excessSequence?.Length ?? 0) + advanced;

        /// <summary>
        /// Gets the memory reserved for the prefix.
        /// </summary>
        public Memory<T> Prefix
        {
            get
            {
                EnsureInitialized(0);
                return prefixMemory;
            }
        }

        /// <inheritdoc />
        public void Advance(int count)
        {
            if (usingExcessMemory)
            {
                excessSequence!.Advance(count);
                realMemory = default;
            }
            else
            {
                realMemory = realMemory.Slice(count);
                advanced += count;
            }
        }

        /// <inheritdoc />
        public Memory<T> GetMemory(int sizeHint = 0)
        {
            EnsureInitialized(sizeHint);
            return realMemory;
        }

        /// <inheritdoc />
        public Span<T> GetSpan(int sizeHint = 0)
        {
            EnsureInitialized(sizeHint);
            return realMemory.Span;
        }

        /// <summary>
        /// Commits all the elements written and the prefix to the underlying writer
        /// and advances the underlying writer past the prefix and payload.
        /// </summary>
        /// <remarks>
        /// This instance is safe to reuse after this call.
        /// </remarks>
        public void Commit()
        {
            if (prefixMemory.Length == 0)
            {
                // No payload was actually written, and we never requested memory, so just write it out.
                innerWriter.Write(Prefix.Span);
            }
            else
            {
                // Payload has been written. Write in the prefix and commit the first buffer.
                innerWriter.Advance(prefixMemory.Length + advanced);

                // Now copy any excess buffer.
                if (usingExcessMemory)
                {
                    Span<T> span = innerWriter.GetSpan((int)excessSequence!.Length);
                    foreach (ReadOnlyMemory<T> segment in excessSequence.AsReadOnlySequence)
                    {
                        segment.Span.CopyTo(span);
                        span = span.Slice(segment.Length);
                    }

                    innerWriter.Advance((int)excessSequence.Length);
                    excessSequence.Reset(); // return backing arrays to memory pools
                }
            }

            // Reset for the next write.
            usingExcessMemory = false;
            prefixMemory = default;
            realMemory = default;
            advanced = 0;
        }

        private void EnsureInitialized(int sizeHint)
        {
            if (prefixMemory.Length == 0)
            {
                int sizeToRequest = expectedPrefixSize + Math.Max(sizeHint, payloadSizeHint == 0 ? PayloadSizeGuess : payloadSizeHint);
                Memory<T> memory = innerWriter.GetMemory(sizeToRequest);
                prefixMemory = memory.Slice(0, expectedPrefixSize);
                realMemory = memory.Slice(expectedPrefixSize);
            }
            else if (realMemory.Length == 0 || realMemory.Length - advanced < sizeHint)
            {
                if (excessSequence == null)
                {
                    excessSequence = new SequenceWriter<T>(memoryPool);
                }

                usingExcessMemory = true;
                realMemory = excessSequence.GetMemory(sizeHint);
            }
        }
    }
}