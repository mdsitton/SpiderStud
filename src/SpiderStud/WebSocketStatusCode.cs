using System;

namespace SpiderStud
{
    public enum WebSocketStatusCode : ushort
    {
        NormalClosure = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        UnsupportedDataType = 1003,
        NoStatusReceived = 1005,
        AbnormalClosure = 1006,
        InvalidFramePayloadData = 1007,
        PolicyViolation = 1008,
        MessageTooBig = 1009,
        MandatoryExt = 1010,
        InternalServerError = 1011,
        ServiceRestart = 1012,
        TryAgain = 1013,
        UpstreamServerError = 1014,
        TLSHandshake = 1015,

        ApplicationUnauthorized = 3000,
        ApplicationError = 4000,
    }

    public static class WebSocketStatusCodeExtensions
    {
        public static bool IsValidCode(this WebSocketStatusCode code)
        {
            ushort codeInt = (ushort)code;

            // Validate any application/library specific codes
            if (codeInt >= 3000 && codeInt <= 4999)
            {
                return true;
            }
            switch (code)
            {
                case WebSocketStatusCode.NormalClosure:
                case WebSocketStatusCode.GoingAway:
                case WebSocketStatusCode.ProtocolError:
                case WebSocketStatusCode.UnsupportedDataType:
                case WebSocketStatusCode.InvalidFramePayloadData:
                case WebSocketStatusCode.PolicyViolation:
                case WebSocketStatusCode.MessageTooBig:
                case WebSocketStatusCode.MandatoryExt:
                case WebSocketStatusCode.InternalServerError:
                case WebSocketStatusCode.ServiceRestart:
                case WebSocketStatusCode.TryAgain:
                case WebSocketStatusCode.UpstreamServerError:
                case WebSocketStatusCode.ApplicationUnauthorized:
                case WebSocketStatusCode.ApplicationError:
                    return true;
                case WebSocketStatusCode.NoStatusReceived:
                case WebSocketStatusCode.AbnormalClosure:
                case WebSocketStatusCode.TLSHandshake:
                default:
                    return false;
            }
        }
    }
}

