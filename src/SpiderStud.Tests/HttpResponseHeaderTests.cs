using System.Diagnostics;
using Xunit;
using System.Text;
using SpiderStud.Http;
using System;
using System.Buffers;
using SoftCircuits.Collections;

namespace SpiderStud.Tests
{
    public class HttpResponseHeaderTests
    {
        [Fact]
        public void ResponseIsValid()
        {
            HttpResponse response = GetResponse();

            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
            response.WriteResponseHeader(writer);
            byte[] writtenData = writer.WrittenSpan.ToArray();
            byte[] real = AsBytes(validResponse);

            Assert.Equal(real, writtenData);
        }

        public byte[] AsBytes(string request)
        {
            return Encoding.UTF8.GetBytes(request);
        }

        public static HttpResponse GetResponse()
        {
            // during tests we care about header ordering for reproducability,
            // but the actual http spec does not care about field order
            var dict = new OrderedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            var response = new HttpResponse(HttpStatusCode.Ok, dict);
            response.Headers["Server"] = "SpiderStud";
            response.Headers["Date"] = "Sun, 29 May 2022 15:58:24 GMT";
            response.Headers["Content-Type"] = "text/plain; charset=UTF-8";
            response.Headers["Content-Length"] = "4";
            response.SetConnection(HttpHeaderConnection.Close);
            return response;
        }

        const string validResponse =
            "HTTP/1.1 200 OK\r\n" +
            "Server: SpiderStud\r\n" +
            "Date: Sun, 29 May 2022 15:58:24 GMT\r\n" +
            "Content-Type: text/plain; charset=UTF-8\r\n" +
            "Content-Length: 4\r\n" +
            "Connection: Close\r\n\r\n";
    }
}
