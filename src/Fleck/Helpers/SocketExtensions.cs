using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using BinaryEx;

namespace Fleck.Helpers
{
    internal static class SocketExtensions
    {
        [ThreadStatic]
        private static byte[] scratchBytes = new byte[size * 3];

        const int size = sizeof(uint);
        public static void SetKeepAlive(this Socket socket, bool on = true, float keepAliveSeconds = 60, float retryIntervalSeconds = 10)
        {
            if (scratchBytes == null)
            {
                scratchBytes = new byte[size * 3];
            }
            uint onVal = (uint)(on ? 1 : 0);
            uint keepAliveVal = (uint)(keepAliveSeconds * 1000);
            uint retryVal = (uint)(retryIntervalSeconds * 1000);

            int pos = 0;
            scratchBytes.WriteUInt32LE(ref pos, onVal);
            scratchBytes.WriteUInt32LE(ref pos, keepAliveVal);
            scratchBytes.WriteUInt32LE(ref pos, retryVal);

            // The tcp keepalive default values on most systems
            // are huge (~7200s). Set them to something more reasonable.
            if (FleckRuntime.IsRunningOnWindows())
                socket.IOControl(IOControlCode.KeepAliveValues, scratchBytes, null);
        }

        public static IPAddress? GetLocalIPAddress(this Socket socket)
        {
            return (socket.LocalEndPoint as IPEndPoint)?.Address;
        }

        public static IPAddress? GetRemoteIPAddress(this Socket socket)
        {
            return (socket.LocalEndPoint as IPEndPoint)?.Address;
        }

        public static int? GetRemotePort(this Socket socket)
        {
            return (socket.RemoteEndPoint as IPEndPoint)?.Port;
        }

        public static int? GetLocalPort(this Socket socket)
        {
            return (socket.LocalEndPoint as IPEndPoint)?.Port;
        }
    }
}