using System;
namespace SpiderStud
{
    public class WebSocketException : Exception
    {
        public WebSocketException(StatusCode statusCode) : base()
        {
            StatusCode = statusCode;
        }

        public WebSocketException(StatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public WebSocketException(StatusCode statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public StatusCode StatusCode { get; private set; }
    }
}
