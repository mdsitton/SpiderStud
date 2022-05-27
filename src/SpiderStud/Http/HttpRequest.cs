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
        public HttpConnection Connection;

        public Dictionary<string, string> Headers;

        private const string pattern =
            @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\s(?<http_version>HTTP\/[0-9]\.[0-9])\r?\n((?<field_name>[^:\r\n]+):[^\S\r\n]*(?<field_value>[^\r\n]*)\r?\n)+";

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public HttpRequest(HttpConnection connection, string method = "", string path = "", string httpVersionStr = "")
        {
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Method = method;
            Path = path;
            HttpVersionStr = httpVersionStr;
            Connection = connection;
        }

        public bool Parse(ReadOnlySpan<byte> bytes)
        {
            // TODO : Throw or return http error return code 413 Payload Too Large
            if (bytes.Length > 4096)
                return false;

            // Check for websocket request header 
            var body = Encoding.ASCII.GetString(bytes); // http header is only ascii data
            Match match = _regex.Match(body);

            if (!match.Success) // Return http error code response
                return false;

            Method = match.Groups["method"].Value;
            Path = match.Groups["path"].Value;
            HttpVersionStr = match.Groups["http_version"].Value;

            var fields = match.Groups["field_name"].Captures;
            var values = match.Groups["field_value"].Captures;
            for (var i = 0; i < fields.Count; i++)
            {
                var name = fields[i].ToString();
                var value = values[i].ToString();
                Headers[name] = value;
            }
            return true;
        }
    }
}
