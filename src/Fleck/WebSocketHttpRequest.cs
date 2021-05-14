using System.Collections.Generic;
using System;

namespace Fleck
{
    public class WebSocketHttpRequest
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public string Scheme { get; set; }

        public string this[string name]
        {
            get
            {
                string value;
                return Headers.TryGetValue(name, out value) ? value : default;
            }
        }

        public IDictionary<string, string> Headers { get; } =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }
}
