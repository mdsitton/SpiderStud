using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Text;
using FastEnumUtility;

namespace SpiderStud.Http
{
    public ref struct HttpResponse
    {
        public string HttpVersionStr => "HTTP/1.1";
        public HttpStatusCode StatusCode;

        public Dictionary<string, string> Headers;
        private Utf8ValueStringBuilder zBuilder;

        public HttpResponse(HttpStatusCode statusCode, HttpHeaderConnection connectionType = HttpHeaderConnection.Close)
        {
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            zBuilder = ZString.CreateUtf8StringBuilder();
            StatusCode = statusCode;
            Headers["Connection"] = FastEnum.GetName(connectionType);
        }

        public ReadOnlySpan<byte> GetResponseBytes()
        {
            zBuilder.AppendFormat("{0} {1} {2}", HttpVersionStr, (int)StatusCode, StatusCode.ToOutputText());
            foreach (var header in Headers)
            {
                zBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }
            zBuilder.Append("\r\n");
            return zBuilder.AsSpan();
        }
    }
}