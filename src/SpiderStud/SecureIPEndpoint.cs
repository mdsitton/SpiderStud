using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SpiderStud
{
    public class SecureIPEndpoint : IPEndPoint
    {
        /// <summary>
        /// If true this endpoint will be utilizing a secure socket
        /// </summary>
        public bool Secure { get; private set; }

        /// <summary>
        /// Creates a new instance of the SecureIPEndpoint class with the specified address and port.
        /// </summary>
        public SecureIPEndpoint(long address, int port, bool secure) : base(address, port)
        {
            Secure = secure;
        }


        /// <summary>
        /// Creates a new instance of the SecureIPEndpoint class with the specified address and port.
        /// </summary>
        public SecureIPEndpoint(IPAddress address, int port, bool secure) : base(address, port)
        {
            Secure = secure;
        }
    }
}