using System;

namespace Fleck
{
    public static class WebSocketStatusCodes
    {
        public const ushort NormalClosure = 1000;
        public const ushort GoingAway = 1001;
        public const ushort ProtocolError = 1002;
        public const ushort UnsupportedDataType = 1003;
        public const ushort NoStatusReceived = 1005;
        public const ushort AbnormalClosure = 1006;
        public const ushort InvalidFramePayloadData = 1007;
        public const ushort PolicyViolation = 1008;
        public const ushort MessageTooBig = 1009;
        public const ushort MandatoryExt = 1010;
        public const ushort InternalServerError = 1011;
        public const ushort ServiceRestart = 1012;
        public const ushort TryAgain = 1013;
        public const ushort UpstreamServerError = 1014;
        public const ushort TLSHandshake = 1015;

        public const ushort ApplicationUnauthorized = 3000;
        public const ushort ApplicationError = 4000;

        public static ushort[] ValidCloseCodes =
        {
            NormalClosure, GoingAway, ProtocolError, UnsupportedDataType,
            InvalidFramePayloadData, PolicyViolation, MessageTooBig,
            MandatoryExt, InternalServerError,
        };
    }
}

