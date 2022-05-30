using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiderStud.Http
{
    public class DefaultHttpServiceHandler : IHttpServiceHandler
    {
        public DefaultHttpServiceHandler()
        {

        }

        public void Dispose()
        {
        }

        public void OnGet(HttpRequest request, HttpConnection connection)
        {
            HttpResponse response = new HttpResponse(HttpStatusCode.Ok);
            response.SetConnection(HttpHeaderConnection.Close);

            connection.SendResponse(response, "hello world!");
        }

        public void OnUnsupported(HttpRequest request, HttpConnection connection)
        {
            HttpResponse response = new HttpResponse(HttpStatusCode.NotImplemented);
            response.SetConnection(HttpHeaderConnection.Close);
            connection.SendResponse(response);
        }

        public bool OnRequest(HttpRequest request, HttpConnection connection)
        {
            switch (request.Method)
            {
                case "GET":
                    OnGet(request, connection);
                    break;
                case "HEAD":
                case "POST":
                case "PUT":
                case "DELETE":
                case "CONNECT":
                case "OPTIONS":
                case "TRACE":
                    OnUnsupported(request, connection);
                    break;
                default:
                    OnUnsupported(request, connection);
                    break;
            }
            return false; // Close connection
        }
    }
}