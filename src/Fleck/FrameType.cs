using System;

namespace Fleck
{
    public enum FrameType : byte
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
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

        public static bool IsControlFrame(this FrameType type)
        {
            // Control frames have 4th bit set within the opcode id
            return ((byte)type & 0x8) == 0x8;
        }
    }
}
