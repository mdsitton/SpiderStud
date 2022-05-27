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
    public struct SequenceOwner : IDisposable
    {
        private readonly SequenceSegment startSegment;
        private readonly SequenceSegment endSegment;
        private SequenceSegment? currentSegment;

        public SequenceSegment First => startSegment;
        public SequenceSegment Last => endSegment;

        public SequenceSegment? Current => currentSegment;

        public void AdvanceCurrentSegment()
        {
            if (currentSegment == null)
                return;
            currentSegment = currentSegment.Next;
        }

        public SequenceOwner(SequenceSegment startSegment, SequenceSegment endSegment)
        {
            this.startSegment = startSegment;
            currentSegment = startSegment;
            this.endSegment = endSegment;
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