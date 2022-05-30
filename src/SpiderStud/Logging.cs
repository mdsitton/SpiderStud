using System;
using System.Diagnostics;

namespace SpiderStud
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
    }
    public class Logging
    {
        public static LogLevel Level = LogLevel.Debug;

        public static Action<LogLevel, string, Exception?> LogAction = (level, message, ex) =>
        {
            Console.WriteLine("{0} [{1}] {2} {3}", DateTime.Now, level, message, ex);
        };

        public static void Warn(string message, Exception ex = null)
        {
            if (Level <= LogLevel.Warn)
                LogAction?.Invoke(LogLevel.Warn, message, ex);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (Level <= LogLevel.Error)
                LogAction?.Invoke(LogLevel.Error, message, ex);
        }

        [Conditional("DEBUG")]
        public static void Debug(string message, Exception ex = null)
        {
            if (Level <= LogLevel.Debug)
                LogAction?.Invoke(LogLevel.Debug, message, ex);
        }

        [Conditional("DEBUG")]
        public static void Info(string message, Exception ex = null)
        {
            if (Level <= LogLevel.Info)
                LogAction?.Invoke(LogLevel.Info, message, ex);
        }
    }
}
