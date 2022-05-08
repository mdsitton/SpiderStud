using System;
using Xunit;
using System.Text;
using System.Collections.Generic;

namespace SpiderStud.Tests
{
    public class DataFrameTests
    {
        [Fact]
        public void ShouldConvertToBytesUnmasked()
        {
            var Payload = Encoding.UTF8.GetBytes("Hello");

            byte[] actual = new byte[7];
            FrameParsing.WriteFrame(actual, Payload, FrameType.Text, true, false);

            var expected = new byte[] { 129, 5, 72, 101, 108, 108, 111 };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldConvertToBytesMasked()
        {
            var Payload = Encoding.UTF8.GetBytes("Hello");

            byte[] key = new byte[] { 61, 84, 35, 6 };
            byte[] actual = new byte[11];
            FrameParsing.WriteFrame(actual, Payload, FrameType.Text, key, true, true);

            var expected = new byte[] { 129, 133, 61, 84, 35, 6, 117, 49, 79, 106, 82 };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldConvertPayloadsOver125BytesToBytes()
        {
            var Payload = Encoding.UTF8.GetBytes(new string('x', 140));

            byte[] actual = new byte[144];
            FrameParsing.WriteFrame(actual, Payload, FrameType.Text, true, false);

            var expectedBuild = new List<byte> { 129, 126, 0, 140 };
            expectedBuild.AddRange(Payload);

            var expected = expectedBuild.ToArray();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PayloadRoundTripLongTest()
        {
            string originalString = new string('x', 131070);
            var Payload = Encoding.UTF8.GetBytes(originalString);

            byte[] data = new byte[131080];
            FrameParsing.WriteFrame(data, Payload, FrameType.Text, true, false);

            var result = FrameParsing.ReadFrameHeader(data);

            Assert.True(result.FrameHeaderFullyParsed);
            Assert.True(result.PayloadReady);
            Assert.False(result.IsMasked);
            Assert.Equal(10, result.PayloadStartOffset);
            Assert.Equal(Payload.Length, result.PayloadLength);

            Span<byte> payload = data.AsSpan().Slice(result.PayloadStartOffset, result.PayloadLength);

            string resultString = Encoding.UTF8.GetString(payload);

            Assert.Equal(originalString, resultString);
        }

        [Fact]
        public void PayloadRoundTripUnmaskedTest()
        {
            string originalString = new string('x', 140);
            var Payload = Encoding.UTF8.GetBytes(originalString);

            byte[] data = new byte[256];
            FrameParsing.WriteFrame(data, Payload, FrameType.Text, true, false);

            var result = FrameParsing.ReadFrameHeader(data);

            Assert.True(result.FrameHeaderFullyParsed);
            Assert.True(result.PayloadReady);
            Assert.False(result.IsMasked);
            Assert.Equal(4, result.PayloadStartOffset);
            Assert.Equal(Payload.Length, result.PayloadLength);

            Span<byte> payload = data.AsSpan().Slice(result.PayloadStartOffset, result.PayloadLength);

            string resultString = Encoding.UTF8.GetString(payload);

            Assert.Equal(originalString, resultString);
        }

        [Fact]
        public void PayloadRoundTripMaskedTest()
        {
            string originalString = "Hello";
            var Payload = Encoding.UTF8.GetBytes(originalString);

            byte[] data = new byte[256];
            FrameParsing.WriteFrame(data, Payload, FrameType.Text, true, true);

            var result = FrameParsing.ReadFrameHeader(data);

            Assert.True(result.FrameHeaderFullyParsed);
            Assert.True(result.PayloadReady);
            Assert.True(result.IsMasked);
            Assert.Equal(6, result.PayloadStartOffset);
            Assert.Equal(Payload.Length, result.PayloadLength);

            Span<byte> payload = data.AsSpan().Slice(result.PayloadStartOffset, result.PayloadLength);
            FrameParsing.ApplyDataMasking(payload, payload, result.MaskKey); // Restore

            string resultString = Encoding.UTF8.GetString(payload);

            Assert.Equal(originalString, resultString);
        }

        [Fact]
        public void PayloadMaskingRoundTripTest()
        {
            const string original = "Hello";

            byte[] key = new byte[] { 61, 84, 35, 6 };

            var bytes = Encoding.UTF8.GetBytes(original);
            byte[] bytes2 = new byte[bytes.Length];
            Array.Copy(bytes, bytes2, bytes.Length);

            // Transform data in place
            FrameParsing.ApplyDataMasking(bytes2, bytes2, key); // Transform
            Assert.NotEqual(bytes, bytes2);
            FrameParsing.ApplyDataMasking(bytes2, bytes2, key); // Restore

            // var decoded = Encoding.UTF8.GetString(bytes);

            Assert.Equal(bytes, bytes2);
        }
    }
}

