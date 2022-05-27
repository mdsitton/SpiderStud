// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on Andrew Arnott's Nerdbank.Streams Sequence<byte> type
// Originally from https://github.com/AArnott/Nerdbank.Streams/blob/main/src/Nerdbank.Streams/Sequence%601.cs
// This version has been modified significantly to function as a stream of sorts, as Segments are filled
// they will be packaged into ReadonlySequence<byte> for consuming by other api's and enables the use of
// sequences across threads for fire-and-forget write operations using background threads.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using SpiderStud.Buffers;

namespace SpiderStud.Buffers
{

    /// <summary>
    /// Manages a sequence of elements, with optional header
    /// </summary>
    /// <remarks>
    /// Instance members are not thread-safe.
    /// </remarks>
    public class SequenceWriter : IBufferWriter<byte>, IDisposable
    {
        // Chose a size of 8000 bytes for our max buffer size
        // This is because after this size on mono buffers get moved to the LOH
        // For coreclr this limit much larger at 85k
        private const int BufferSize = 8000;

        private static readonly ReadOnlySequence<byte> Empty = new ReadOnlySequence<byte>(SequenceSegment.Empty, 0, SequenceSegment.Empty, 0);

        private static readonly ConcurrentQueue<SequenceSegment> segmentPool = new ConcurrentQueue<SequenceSegment>();

        private readonly MemoryPool<byte>? memoryPool = MemoryPool<byte>.Shared;

        private SequenceSegment? first;
        private SequenceSegment? last;
        private SequenceSegment? header;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceWriter"/> class
        /// </summary>
        public SequenceWriter()
        {
        }

        public int HeaderLength => header?.Length ?? 0;

        /// <summary>
        /// Gets the length of the sequence.
        /// </summary>
        public long Length
        {
            get
            {
                long length = 0;

                var current = first;
                while (current != null && current != last)
                {
                    length += current.Length;
                    current = current.Next;
                }
                return length;
            }
        }

        /// <summary>
        /// Return all current written data as <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        public ReadOnlySequence<byte> WrittenSequence =>
            this.first is { } first && this.last is { } last ? // check not null
                new ReadOnlySequence<byte>(first, first.Start, last, last!.End) : Empty;


        /// <summary>
        /// Commits all written sequence data including header data and resets SequenceWriter instance for reuse 
        /// </summary>
        /// <returns><see cref="SequenceOwner"/> container used to manage <see cref="SequenceSegment"/> pooling, must be disposed</returns>
        public SequenceOwner SequenceCommit()
        {
            if (first == null || last == null)
            {
                throw new InvalidOperationException("No sequence memory ready to commit");
            }
            var firstSequence = first;
            if (header != null)
            {
                header.Next = first;
                firstSequence = header;

                // Update full sequence lengths to account for the header
                var current = header;
                while (current != null)
                {
                    current.UpdateLength();
                    current = current.Next;
                }

            }
            var sequence = new SequenceOwner(firstSequence, last);
            header = first = last = null;
            return sequence;
        }

        /// <summary>
        /// Advances the sequence to include the specified number of elements initialized into memory
        /// returned by a prior call to <see cref="GetMemory(int)"/>.
        /// </summary>
        /// <param name="count">The number of elements written into memory.</param>
        public void Advance(int count)
        {
            if (last == null)
            {
                throw new InvalidOperationException("Cannot advance before acquiring memory.");
            }
            last.Advance(count);
        }

        /// <summary>
        /// Advances the header sequence to include the specified number of elements initialized into memory
        /// returned by a prior call to <see cref="GetHeaderMemory(int)"/>.
        /// </summary>
        /// <param name="count">The number of elements written into memory.</param>
        public void AdvanceHeader(int count)
        {
            if (header == null)
            {
                throw new InvalidOperationException("Cannot advance before acquiring header memory.");
            }
            header.Advance(count);
        }

        /// <summary>
        /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Memory<byte> GetMemory(int sizeHint) => GetSegment(sizeHint).RemainingMemory;

        /// <summary>
        /// Gets writable memory that can be initialized and added to the header sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// Note that header is only a single sequence so it cannot allocate multiple segments, so this may fail after multiple calls
        /// due to the internal buffer size being insuffecient for the requested size.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Memory<byte> GetHeaderMemory(int sizeHint) => GetHeaderSegment(sizeHint).RemainingMemory;

        /// <summary>
        /// Gets writable span that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// Note that header is only a single sequence so it cannot allocate multiple segments, so this may fail after multiple calls
        /// due to the internal buffer size being insuffecient for the requested size.
        /// </summary>
        /// <param name="sizeHint">The size of the span required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested span.</returns>
        public Span<byte> GetHeaderSpan(int sizeHint) => GetHeaderSegment(sizeHint).RemainingSpan;

        /// <summary>
        /// Gets writable span that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the span required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested span.</returns>
        public Span<byte> GetSpan(int sizeHint) => GetSegment(sizeHint).RemainingSpan;

        /// <summary>
        /// Clears the entire sequence, recycles associated memory into pools,
        /// and resets this instance for reuse.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        public void Dispose() => Reset();

        /// <summary>
        /// Clears the entire sequence and recycles associated memory into pools.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        public void Reset()
        {
            var current = first;
            while (current != null)
            {
                current = RecycleAndGetNext(current);
            }

            header = first = last = null;
        }

        private SequenceSegment GetHeaderSegment(int sizeHint)
        {
            if (sizeHint >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            if (header == null)
            {
                if (sizeHint == 0)
                {
                    // We're going to need more memory. Take whatever size the pool wants to give us.
                    sizeHint = -1;
                }
                else
                {
                    sizeHint = sizeHint > BufferSize ? sizeHint : BufferSize;
                }
                if (!segmentPool.TryDequeue(out header))
                {
                    header = new SequenceSegment();
                }
                header.Assign(memoryPool!.Rent(sizeHint));
            }
            else if (header.WritableBytes >= sizeHint)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            return header;
        }

        private SequenceSegment GetSegment(int sizeHint)
        {
            if (sizeHint >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            if (last == null || last.WritableBytes < sizeHint)
            {
                if (sizeHint == 0)
                {
                    // We're going to need more memory. Take whatever size the pool wants to give us.
                    sizeHint = -1;
                }
                else
                {
                    sizeHint = sizeHint > BufferSize ? sizeHint : BufferSize;
                }

                if (!segmentPool.TryDequeue(out SequenceSegment segment))
                {
                    segment = new SequenceSegment();
                }
                segment.Assign(memoryPool!.Rent(sizeHint));

                Append(segment);
            }
            return last!;
        }

        private void Append(SequenceSegment segment)
        {
            // We don't have any elements yet set the first one
            if (last == null)
            {
                first = last = segment;
                return;
            }

            SequenceSegment previous = last!;

            // The last block is completely unused. Replace it instead of appending to it.
            if (previous.Length == 0)
            {
                // Find the previous segment from last
                previous = first!;
                if (first == last)
                {
                    first = segment;
                }
                else
                {
                    while (previous!.Next != last)
                    {
                        previous = previous!.Next!;
                    }
                }

                // Recycle last
                var next = RecycleAndGetNext(last);

                // The last element should not have a next element
                if (next != null)
                {
                    throw new InvalidOperationException("Last element has next element");
                }
            }
            previous.SetNext(segment);

            last = segment;
        }

        internal static SequenceSegment? RecycleAndGetNext(SequenceSegment segment)
        {
            var recycledSegment = segment;
            var nextSegment = segment.Next;
            recycledSegment.ResetMemory();
            segmentPool.Enqueue(recycledSegment);
            return nextSegment;
        }
    }
}