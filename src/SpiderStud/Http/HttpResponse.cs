using System;
using System.Buffers;
using System.Collections.Generic;
using FastEnumUtility;
using System.Text;
using System.Buffers.Text;

namespace SpiderStud.Http
{
    public ref struct HttpResponse
    {
        private const string versionStr = "HTTP/1.1";
        public string HttpVersionStr => versionStr;
        public HttpStatusCode StatusCode;

        public Dictionary<string, string> Headers;

        const string seperator = ": ";
        const string newLine = "\r\n";

        public HttpResponse(HttpStatusCode statusCode, HttpHeaderConnection connectionType = HttpHeaderConnection.Close)
        {
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            StatusCode = statusCode;
            Headers["Connection"] = FastEnum.GetName(connectionType);
        }

        /// <summary>
        /// Write response header directly without any intermediate copies
        /// </summary>
        public void WriteResponseHeader(IBufferWriter<byte> writer)
        {
            WriteHeaderStatusLine(writer);

            foreach (var key in Headers.Keys)
            {
                WriteHeaderField(writer, key, Headers[key]);
            }
            WriteEndOfHeader(writer);
        }

        // Write out end of header without any extra intermediate copies
        private void WriteEndOfHeader(IBufferWriter<byte> writer)
        {
            var writeSpan = writer.GetSpan(2);

            // Write new line
            writer.Advance(Encoding.ASCII.GetBytes(newLine, writeSpan));
        }

        // Write out header status line without any extra intermediate copies
        private void WriteHeaderStatusLine(IBufferWriter<byte> writer)
        {
            string statusText = StatusCode.ToOutputText();
            var writeSpan = writer.GetSpan(versionStr.Length + 8 + statusText.Length);

            // Write http version
            int writtenLength = Encoding.ASCII.GetBytes(versionStr, writeSpan);
            writeSpan[writtenLength++] = (byte)' ';
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write status code int
            Utf8Formatter.TryFormat((int)StatusCode, writeSpan, out writtenLength);
            writeSpan[writtenLength++] = (byte)' ';
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write status text
            writtenLength = Encoding.ASCII.GetBytes(statusText, writeSpan);
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write new line
            writtenLength = Encoding.ASCII.GetBytes(newLine, writeSpan);
            writer.Advance(writtenLength);
        }

        // Write out field without any extra intermediate copies
        private void WriteHeaderField(IBufferWriter<byte> writer, string key, string value)
        {
            var writeSpan = writer.GetSpan(key.Length + value.Length + seperator.Length + newLine.Length);

            // Write key
            int writtenLength = Encoding.ASCII.GetBytes(key, writeSpan);
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write seperator
            writtenLength = Encoding.ASCII.GetBytes(seperator, writeSpan);
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write value
            writtenLength = Encoding.ASCII.GetBytes(value, writeSpan);
            writeSpan = writeSpan.Slice(writtenLength);
            writer.Advance(writtenLength);

            // Write new line
            writtenLength = Encoding.ASCII.GetBytes(newLine, writeSpan);
            writer.Advance(writtenLength);
        }
    }
}