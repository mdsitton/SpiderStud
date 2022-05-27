using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud.Buffers
{
    /// <summary>
    /// Used to recycle shared <see cref="SequenceSegment"/> instances
    /// once the <see cref="ReadOnlySequence<byte>"> is ready to be discarded
    /// </summary>
    public readonly struct ReadOnlySequenceOwner : IDisposable
    {
        internal static ReadOnlySequenceOwner Empty = new ReadOnlySequenceOwner();
        private readonly SequenceSegment startSegment;
        private readonly int startIndex;
        private readonly SequenceSegment endSegment;
        private readonly int endIndex;
        readonly ReadOnlySequence<byte> sequence;

        public ReadOnlySequence<byte> Sequence => sequence;

        public ReadOnlySequenceOwner(SequenceSegment startSegment, int startIndex, SequenceSegment endSegment, int endIndex)
        {
            this.startSegment = startSegment;
            this.startIndex = startIndex;
            this.endSegment = endSegment;
            this.endIndex = endIndex;
            sequence = new ReadOnlySequence<byte>(startSegment, startIndex, endSegment, endIndex);
        }

        public void Dispose()
        {
            // Loop over all segments and recycle them.
            var current = startSegment;
            while (current != null)
            {
                current = SequenceWriter.RecycleAndGetNext(current);
            }
        }
    }
}