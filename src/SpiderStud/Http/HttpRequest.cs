using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;

namespace SpiderStud.Http
{
    public ref struct HttpRequest
    {
        public string Method;
        public string Path;
        public string HttpVersionStr;
        public HttpConnection? Connection;
        public int HeaderStart;
        public int HeaderLength;

        public Dictionary<string, string> Headers;

        public HttpRequest(HttpConnection? connection)
        {
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Method = string.Empty;
            Path = string.Empty;
            HttpVersionStr = string.Empty;
            Connection = connection;
            HeaderStart = 0;
            HeaderLength = 0;
        }

        // This is the whitespace characters defined as allowed
        private static readonly byte[] whiteSpace = new byte[] {
            (byte)' ',
            (byte)'\t',
        };

        private static readonly byte[] endOfLineSequence = new byte[] {
            (byte)'\r',
            (byte)'\n'};

        private static readonly byte[] endOfHeaderSequence = new byte[] {
            (byte)'\r',
            (byte)'\n',
            (byte)'\r',
            (byte)'\n'};

        private const byte kvSeperator = (byte)':';

        private int SkipWhitespace(ReadOnlySpan<byte> data, int startPos)
        {
            ReadOnlySpan<byte> whitespaceSpan = whiteSpace;
            for (int i = startPos; i < data.Length; ++i)
            {
                int found = whitespaceSpan.IndexOf(data[i]);
                if (found == -1)
                {
                    return i;
                }
            }
            return -1;
        }

        private int SkipWhitespaceFromEnd(ReadOnlySpan<byte> data)
        {
            ReadOnlySpan<byte> whitespaceSpan = whiteSpace;
            for (int i = data.Length - 1; i >= 0; --i)
            {
                int found = whitespaceSpan.IndexOf(data[i]);
                if (found == -1)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool ParseRequestLine(ReadOnlySpan<byte> line)
        {
            ReadOnlySpan<byte> whitespaceSpan = whiteSpace;

            // Method string
            int nextWhiteSpace = line.IndexOfAny(whitespaceSpan);
            if (nextWhiteSpace == -1) return false;

            int endOfWhiteSpace = SkipWhitespace(line, nextWhiteSpace);
            if (endOfWhiteSpace == -1) return false;

            Method = Encoding.ASCII.GetString(line.Slice(0, nextWhiteSpace));
            line = line.Slice(endOfWhiteSpace);
            Console.WriteLine(Method);

            // Path string
            nextWhiteSpace = line.IndexOfAny(whitespaceSpan);
            if (nextWhiteSpace == -1) return false;

            endOfWhiteSpace = SkipWhitespace(line, nextWhiteSpace);
            if (endOfWhiteSpace == -1) return false;

            Path = Encoding.ASCII.GetString(line.Slice(0, nextWhiteSpace));
            line = line.Slice(endOfWhiteSpace);
            Console.WriteLine(Path);

            // http version string
            HttpVersionStr = Encoding.ASCII.GetString(line);
            if (!HttpVersionStr.StartsWith("HTTP"))
                return false;
            return true;
        }

        public bool ParseHeaderFields(ReadOnlySpan<byte> line)
        {
            string lineStr = Encoding.ASCII.GetString(line);
            ReadOnlySpan<byte> whitespaceSpan = whiteSpace;
            int sepIndex = line.IndexOf(kvSeperator);

            if (sepIndex == -1)
                return false;

            // Verify that we don't have whitespace at the start of header fields
            // Any such data is use of obsolete line folding, and we are required to fail here
            int found = whitespaceSpan.IndexOf(line[0]);
            if (found != -1)
                return false;

            string key = Encoding.ASCII.GetString(line.Slice(0, sepIndex));
            line = line.Slice(sepIndex + 1); // skip over seperator
            int endOfWhiteSpace = SkipWhitespace(line, 0);

            string value;

            // This will only happen if there was a key with no value
            if (endOfWhiteSpace == -1)
            {
                value = string.Empty;
            }
            else
            {
                int length = SkipWhitespaceFromEnd(line) + 1;
                value = Encoding.ASCII.GetString(line.Slice(endOfWhiteSpace, length - endOfWhiteSpace));
            }

            Headers.Add(key, value);

            return true;
        }

        public HttpStatusCode Parse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= 8000)
                return HttpStatusCode.PayloadTooLarge;

            if (bytes.Length == 0)
                return HttpStatusCode.BadRequest;

            // Spec specifies that for request parsing robustness we:
            //    SHOULD ignore at least one empty line (CRLF) received prior to the request-line.
            if (bytes.StartsWith(endOfLineSequence))
            {
                HeaderStart += 2;
            }

            int endIndex = bytes.IndexOf(endOfHeaderSequence);

            // If we haven't found the end of the request header exit
            if (endIndex == -1)
            {
                return HttpStatusCode.BadRequest;
            }

            HeaderLength = endIndex - HeaderStart + endOfHeaderSequence.Length;

            // Make sure we slice off any request body data 
            bytes = bytes.Slice(HeaderStart, HeaderLength);

            // headers do not allow null bytes reject this request
            if (bytes.IndexOf((byte)'\0') != -1)
                return HttpStatusCode.BadRequest;

            bool parsedRequestLine = false;

            while (bytes.Length > 4)
            {
                int lineEnd = bytes.IndexOf(endOfLineSequence);
                ReadOnlySpan<byte> line = bytes.Slice(0, lineEnd);
                if (!parsedRequestLine)
                {
                    if (!ParseRequestLine(line))
                    {
                        return HttpStatusCode.BadRequest;
                    }
                    parsedRequestLine = true;
                }
                else
                {
                    if (!ParseHeaderFields(line))
                    {
                        return HttpStatusCode.BadRequest;
                    }
                }
                bytes = bytes.Slice(lineEnd + 2);
            }

            // If the request doesn't have any headers return BadRequest
            if (Headers.Count == 0)
            {
                return HttpStatusCode.BadRequest;
            }
            return HttpStatusCode.Ok;
        }
    }
}
