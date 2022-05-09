using System;
namespace SpiderStud
{
    public class WebSocketException : Exception
    {
        public WebSocketException(WebSocketStatusCode statusCode) : base()
        {
            StatusCode = statusCode;
        }

        public WebSocketException(WebSocketStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public WebSocketException(WebSocketStatusCode statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public WebSocketStatusCode StatusCode { get; private set; }
    }
}
