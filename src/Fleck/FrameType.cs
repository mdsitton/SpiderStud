using System;

namespace Fleck
{
    public enum FrameType : byte
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }

    public static class FrameTypeExtensions
    {
        public static bool IsDefined(this FrameType type)
        {
            return type == FrameType.Continuation ||
                   type == FrameType.Text ||
                   type == FrameType.Binary ||
                   type == FrameType.Close ||
                   type == FrameType.Ping ||
                   type == FrameType.Pong;
        }
    }
}
