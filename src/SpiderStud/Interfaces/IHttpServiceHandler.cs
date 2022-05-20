using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud.Http
{
    public interface IHttpServiceHandler : IDisposable
    {
        void OnStart(SpiderStudServer server);
        void OnRequest(HttpRequest request);
    }
}