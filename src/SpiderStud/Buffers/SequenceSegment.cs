// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on Andrew Arnott's Nerdbank.Streams Sequence<byte> type
// This version has been made to have a static pool of SequenceSegment instances
//
// from https://github.com/AArnott/Nerdbank.Streams/blob/main/src/Nerdbank.Streams/Sequence%601.cs

using System;
using System.Buffers;
using System.Diagnostics;

namespace SpiderStud
{
    public class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        internal static readonly SequenceSegment Empty = new SequenceSegment();

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data starts.
        /// </summary>
        /// <remarks>This may be nonzero as a result of calling <see cref="SequenceWriter{T}.AdvanceTo(SequencePosition)"/>.</remarks>
        internal int Start { get; private set; }

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data ends.
        /// </summary>
        internal int End { get; private set; }

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Memory<byte> RemainingMemory => AvailableMemory.Slice(End);

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Span<byte> RemainingSpan => AvailableMemory.Span.Slice(End);

        /// <summary>
        /// Gets the full memory owned by the <see cref="memoryOwner"/>.
        /// </summary>
        internal Memory<byte> AvailableMemory => memoryOwner?.Memory ?? default;

        /// <summary>
        /// Gets the number of elements that are committed in this segment.
        /// </summary>
        internal int Length => End - Start;

        /// <summary>
        /// Gets the amount of writable bytes in this segment.
        /// It is the amount of bytes between <see cref="Length"/> and <see cref="End"/>.
        /// </summary>
        internal int WritableBytes => AvailableMemory.Length - End;

        private IMemoryOwner<byte>? memoryOwner;

        /// <summary>
        /// Gets or sets the next segment in the singly linked list of segments.
        /// </summary>
        internal new SequenceSegment? Next
        {
            get => (SequenceSegment?)base.Next;
            set => base.Next = value;
        }

        /// <summary>
        /// Assigns this (recyclable) segment a new area in memory.
        /// </summary>
        /// <param name="memoryOwner">The memory and a means to recycle it.</param>
        internal void Assign(IMemoryOwner<byte> memoryOwner)
        {
            this.memoryOwner = memoryOwner;
            Memory = memoryOwner.Memory;
        }

        /// <summary>
        /// Clears all fields in preparation to recycle this instance.
        /// </summary>
        internal void ResetMemory()
        {
            Memory = default;
            Next = null;
            RunningIndex = 0;
            Start = 0;
            End = 0;
            memoryOwner?.Dispose();
            memoryOwner = null;
        }

        internal void UpdateLength()
        {
            if (Next != null)
            {
                Next.RunningIndex = RunningIndex + Start + Length;
            }
        }

        /// <summary>
        /// Adds a new segment after this one.
        /// </summary>
        /// <param name="segment">The next segment in the linked list.</param>
        internal void SetNext(SequenceSegment segment)
        {
            Next = segment;
            UpdateLength();

            // Trim any slack on this segment.

            // When setting Memory, we start with index 0 instead of rt because
            // the first segment has an explicit index set anyway,
            // and we don't want to double-count it here.
            Memory = AvailableMemory.Slice(0, Start + Length);
        }

        /// <summary>
        /// Commits more elements as written in this segment.
        /// </summary>
        /// <param name="count">The number of elements written.</param>
        internal void Advance(int count)
        {
            if (!(count >= 0 && End + count <= Memory.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            End += count;
        }

        /// <summary>
        /// Removes some elements from the start of this segment.
        /// </summary>
        /// <param name="offset">The number of elements to ignore from the start of the underlying array.</param>
        internal void AdvanceTo(int offset)
        {
            Debug.Assert(offset >= Start, "Trying to rewind.");
            Start = offset;
        }
    }
}