using System;

namespace SpiderStud
{
    public enum StatusCode : ushort
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

    public static class StatusCodeExtensions
    {
        public static bool IsValidCode(this StatusCode code)
        {
            ushort codeInt = (ushort)code;

            // Validate any application/library specific codes
            if (codeInt >= 3000 && codeInt <= 4999)
            {
                return true;
            }
            switch (code)
            {
                case StatusCode.NormalClosure:
                case StatusCode.GoingAway:
                case StatusCode.ProtocolError:
                case StatusCode.UnsupportedDataType:
                case StatusCode.InvalidFramePayloadData:
                case StatusCode.PolicyViolation:
                case StatusCode.MessageTooBig:
                case StatusCode.MandatoryExt:
                case StatusCode.InternalServerError:
                case StatusCode.ServiceRestart:
                case StatusCode.TryAgain:
                case StatusCode.UpstreamServerError:
                case StatusCode.ApplicationUnauthorized:
                case StatusCode.ApplicationError:
                    return true;
                case StatusCode.NoStatusReceived:
                case StatusCode.AbnormalClosure:
                case StatusCode.TLSHandshake:
                default:
                    return false;
            }
        }
    }
}

