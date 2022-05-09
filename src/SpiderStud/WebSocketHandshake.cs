using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SpiderStud.Http;

namespace SpiderStud
{
    public static class WebSocketHandshake
    {
        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly ThreadLocal<SHA1> sha1Hash = new ThreadLocal<SHA1>(() => SHA1.Create());

        internal static string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var bytes = sha1Hash.Value.ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }

        internal static ReadOnlySpan<byte> CreateHandshake(HttpRequest request)
        {
            SpiderStudLog.Debug("Building Hybi-14 Response");
            HttpResponse response = new HttpResponse(HttpStatusCode.SwitchingProtocols, HttpHeaderConnection.Upgrade);
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Sec-WebSocket-Accept"] = CreateResponseKey(request.Headers["Sec-WebSocket-Key"]);

            return response.GetResponseBytes();
        }
    }
}