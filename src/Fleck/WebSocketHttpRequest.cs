using System.Collections.Generic;
using System;

namespace Fleck
{
    public class WebSocketHttpRequest
    {
        public string Method { get; set; } = String.Empty;

        public string Path { get; set; } = String.Empty;

        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }
}
