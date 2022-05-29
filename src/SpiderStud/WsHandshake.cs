using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SpiderStud.Http;
using System.Buffers;
using System.Buffers.Text;

namespace SpiderStud
{
    public static class WsHandshake
    {
        private static readonly byte[] wsResponseGuid = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        private static readonly ThreadLocal<SHA1> sha1Hash = new ThreadLocal<SHA1>(() => SHA1.Create());

        // The request key is defined as a 16-byte nonce which when encoded
        // as base64 should always result in a 24-byte ascii sequence
        // if the request key is of any other length we should reject the handshake
        private const int base64RequestKeyLength = 24;
        private const int responseGuidLength = 36;

        private static string CreateResponseKey(string requestKey)
        {
            Span<byte> scratch = stackalloc byte[base64RequestKeyLength + responseGuidLength];
            ReadOnlySpan<byte> guid = wsResponseGuid;

            // Concatinate requestKey and responseGuid
            Encoding.ASCII.GetBytes(requestKey, scratch);
            guid.CopyTo(scratch.Slice(base64RequestKeyLength));

            Span<byte> hash = stackalloc byte[20]; // sha-1 hash is 160-bits

            if (!sha1Hash.Value.TryComputeHash(scratch, hash, out int bytesWritten) && bytesWritten != 20)
            {
                return string.Empty;
            }

            return Convert.ToBase64String(hash);
        }

        internal static HttpResponse CreateHandshake(HttpRequest request)
        {
            HttpResponse response = new HttpResponse(HttpStatusCode.SwitchingProtocols, HttpHeaderConnection.Upgrade);
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Sec-WebSocket-Accept"] = CreateResponseKey(request.Headers["Sec-WebSocket-Key"]);

            return response;
        }
    }
}