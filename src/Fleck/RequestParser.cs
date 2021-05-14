using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Fleck
{
    public class RequestParser
    {
        private const string pattern =
            @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n((?<field_name>[^:\r\n]+):[^\S\r\n]*(?<field_value>[^\r\n]*)\r\n)+";

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static WebSocketHttpRequest Parse(ArraySegment<byte> bytes)
        {
            return Parse(bytes, "ws");
        }

        public static WebSocketHttpRequest Parse(ArraySegment<byte> bytes, string scheme)
        {
            if (bytes.Count > 4096)
                return null;

            // Check for websocket request header
            var body = Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
            Match match = _regex.Match(body);

            if (!match.Success)
                return null;

            var request = new WebSocketHttpRequest
            {
                Method = match.Groups["method"].Value,
                Path = match.Groups["path"].Value,
                Scheme = scheme
            };

            var fields = match.Groups["field_name"].Captures;
            var values = match.Groups["field_value"].Captures;
            for (var i = 0; i < fields.Count; i++)
            {
                var name = fields[i].ToString();
                var value = values[i].ToString();
                request.Headers[name] = value;
            }

            return request;
        }
    }
}

