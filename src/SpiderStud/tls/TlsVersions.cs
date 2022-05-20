using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SpiderStud.Tls
{
    [Flags]
    public enum TlsVersions
    {
        Tls12 = 64,
        Tls13 = 128,
    }
}