namespace SpiderStud.Http
{
    public enum HttpStatusCode
    {
        Continue = 100,
        SwitchingProtocols = 101,
        Ok = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultipleChoices = 300,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,
        TemporaryRedirect = 307,
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        PayloadTooLarge = 413,
        UriTooLong = 414,
        UnsupportedMediaType = 415,
        RequestedRangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        UpgradeRequired = 426,
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HttpVersionNotSupported = 505,
    }

    public static class StatusCodeExtensions
    {
        public static string ToOutputText(this HttpStatusCode code)
        {
            switch (code)
            {
                case HttpStatusCode.Continue:
                    return "Continue";
                case HttpStatusCode.SwitchingProtocols:
                    return "Switching Protocols";
                case HttpStatusCode.Ok:
                    return "OK";
                case HttpStatusCode.Created:
                    return "Created";
                case HttpStatusCode.Accepted:
                    return "Accepted";
                case HttpStatusCode.NonAuthoritativeInformation:
                    return "Non-Authoritative Information";
                case HttpStatusCode.NoContent:
                    return "No Content";
                case HttpStatusCode.ResetContent:
                    return "Reset Content";
                case HttpStatusCode.PartialContent:
                    return "Partial Content";
                case HttpStatusCode.MultipleChoices:
                    return "Multiple Choices";
                case HttpStatusCode.MovedPermanently:
                    return "Moved Permanently";
                case HttpStatusCode.Found:
                    return "Found";
                case HttpStatusCode.SeeOther:
                    return "See Other";
                case HttpStatusCode.NotModified:
                    return "Not Modified";
                case HttpStatusCode.UseProxy:
                    return "Use Proxy";
                case HttpStatusCode.TemporaryRedirect:
                    return "Temporary Redirect";
                case HttpStatusCode.BadRequest:
                    return "Bad Request";
                case HttpStatusCode.Unauthorized:
                    return "Unauthorized";
                case HttpStatusCode.PaymentRequired:
                    return "Payment Required";
                case HttpStatusCode.Forbidden:
                    return "Forbidden";
                case HttpStatusCode.NotFound:
                    return "Not Found";
                case HttpStatusCode.MethodNotAllowed:
                    return "Method Not Allowed";
                case HttpStatusCode.NotAcceptable:
                    return "Not Acceptable";
                case HttpStatusCode.ProxyAuthenticationRequired:
                    return "Proxy Authentication Required";
                case HttpStatusCode.RequestTimeout:
                    return "Request Time-out";
                case HttpStatusCode.Conflict:
                    return "Conflict";
                case HttpStatusCode.Gone:
                    return "Gone";
                case HttpStatusCode.LengthRequired:
                    return "Length Required";
                case HttpStatusCode.PreconditionFailed:
                    return "Precondition Failed";
                case HttpStatusCode.PayloadTooLarge:
                    return "Payload Too Large";
                case HttpStatusCode.UriTooLong:
                    return "URI Too Long";
                case HttpStatusCode.UnsupportedMediaType:
                    return "Unsupported Media Type";
                case HttpStatusCode.RequestedRangeNotSatisfiable:
                    return "Requested range not satisfiable";
                case HttpStatusCode.ExpectationFailed:
                    return "Expectation Failed";
                case HttpStatusCode.UpgradeRequired:
                    return "Upgrade Required";
                case HttpStatusCode.InternalServerError:
                    return "Internal Server Error";
                case HttpStatusCode.NotImplemented:
                    return "Not Implemented";
                case HttpStatusCode.BadGateway:
                    return "Bad Gateway";
                case HttpStatusCode.ServiceUnavailable:
                    return "Service Unavailable";
                case HttpStatusCode.GatewayTimeout:
                    return "Gateway Time-out";
                case HttpStatusCode.HttpVersionNotSupported:
                    return "HTTP Version not supported";
                default:
                    return "Unknown";
            }
        }
    }
}