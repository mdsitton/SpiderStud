using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud.Http
{
    public interface IHttpServiceHandler : IDisposable
    {
        /// <summary>
        /// When an http request is recieved
        /// </summary>
        /// <param name="request">http headers</param>
        /// <param name="connection">Active http connection, used to read request or send response data</param>
        /// <returns>If the http connection should be kept alive after this request has been handled</returns>
        bool OnRequest(HttpRequest request, HttpConnection connection);
    }
}