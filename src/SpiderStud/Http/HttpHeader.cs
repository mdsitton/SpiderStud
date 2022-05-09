using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Text;

namespace SpiderStud.Http
{
    public static class HttpHeader
    {
        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly ThreadLocal<SHA1> sha1Hash = new ThreadLocal<SHA1>(() => SHA1.Create());
        private static Utf8ValueStringBuilder zBuilder = ZString.CreateUtf8StringBuilder();

        private const string pattern =
            @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n((?<field_name>[^:\r\n]+):[^\S\r\n]*(?<field_value>[^\r\n]*)\r\n)+";

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);


        internal static string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var bytes = sha1Hash.Value.ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }

        internal static ReadOnlySpan<byte> CreateHandshake(HttpRequest request)
        {
            SpiderStudLog.Debug("Building Hybi-14 Response");
            zBuilder.Clear();

            zBuilder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            zBuilder.Append("Upgrade: websocket\r\n");
            zBuilder.Append("Connection: Upgrade\r\n");

            if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out string val))
            {
                // Stop processing handshake and return http 400 error
                // throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
            }
            string responseKey = CreateResponseKey(val);
            zBuilder.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", responseKey);
            zBuilder.Append("\r\n");

            return zBuilder.AsSpan();
        }

        public static HttpRequest? Parse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > 4096)
                return null;

            // Check for websocket request header
            var body = Encoding.UTF8.GetString(bytes);
            Match match = _regex.Match(body);

            if (!match.Success)
                return null;

            var request = new HttpRequest
            {
                Method = match.Groups["method"].Value,
                Path = match.Groups["path"].Value,
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