using System;
using NUnit.Framework;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Fleck;

namespace Fleck.Tests
{
    [TestFixture]
    public class DataFrameTests
    {
        [Test]
        public void ShouldConvertToBytes()
        {
            var Payload = Encoding.UTF8.GetBytes("Hello");

            byte[] actual = new byte[7];
            FrameParsing.WriteFrame(actual, Payload, FrameType.Text, true, false);

            var expected = new byte[] { 129, 5, 72, 101, 108, 108, 111 };

            Assert.AreEqual(expected, actual);
        }


        [Test]
        public void ShouldConvertPayloadsOver125BytesToBytes()
        {
            var Payload = Encoding.UTF8.GetBytes(new string('x', 140));

            byte[] actual = new byte[144];
            FrameParsing.WriteFrame(actual, Payload, FrameType.Text, true, false);

            var expectedBuild = new List<byte> { 129, 126, 0, 140 };
            expectedBuild.AddRange(Payload);

            var expected = expectedBuild.ToArray();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void PayloadRoundTripUnmaskedTest()
        {
            string originalString = new string('x', 140);
            var Payload = Encoding.UTF8.GetBytes(originalString);

            byte[] data = new byte[144];
            FrameParsing.WriteFrame(data, Payload, FrameType.Text, true, false);

            var result = FrameParsing.ReadFrameHeader(data);

            Assert.IsTrue(result.FrameHeaderFullyParsed);
            Assert.IsTrue(result.PayloadReady);

            Span<byte> payload = data.AsSpan().Slice(result.PayloadStartOffset);

            string resultString = Encoding.UTF8.GetString(payload);

            Assert.AreEqual(originalString, resultString);
        }

        [Test]
        public void PayloadRoundTripMaskedTest()
        {
            string originalString = new string('x', 140);
            var Payload = Encoding.UTF8.GetBytes(originalString);

            byte[] data = new byte[144];
            FrameParsing.WriteFrame(data, Payload, FrameType.Text, true, true);

            var result = FrameParsing.ReadFrameHeader(data);

            Assert.IsTrue(result.FrameHeaderFullyParsed);
            Assert.IsTrue(result.PayloadReady);

            Span<byte> payload = data.AsSpan().Slice(result.PayloadStartOffset);

            string resultString = Encoding.UTF8.GetString(payload);

            Assert.AreEqual(originalString, resultString);
        }

        [Test]
        public void PayloadMaskingRoundTripTest()
        {
            const string original = "Whoa";

            byte[] key = new byte[] { 0x51, 0x14, 0x81, 0x5a };

            var bytes = Encoding.UTF8.GetBytes(original);

            // Transform data in place
            FrameParsing.ApplyDataMasking(bytes, bytes, key); // Transform
            FrameParsing.ApplyDataMasking(bytes, bytes, key); // Restore

            var decoded = Encoding.UTF8.GetString(bytes);

            Assert.AreEqual(original, decoded);
        }
    }
}

