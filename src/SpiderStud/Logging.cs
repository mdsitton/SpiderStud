using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SpiderStud
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Exception,
    }
    public class Logging
    {
        public static LogLevel Level = LogLevel.Debug;

        public static Action<string> LogAction = (message) =>
        {
            Console.WriteLine(message);
        };

        [ThreadStatic]
        private static StringBuilder builder = new StringBuilder();
        private const string topLevelPath = "\\src\\SpiderStud\\";
        public static void FormatPrefix(LogLevel level,
            string memberName,
            string sourceFilePath,
            int sourceLineNumber)
        {
            ReadOnlySpan<char> path = sourceFilePath;
            var index = path.IndexOf(topLevelPath);
            var newPath = path.Slice(index + topLevelPath.Length).ToString();
            builder.AppendFormat("{0} [{1}] ", DateTime.Now, level);
            builder.AppendFormat("[{0}:{1}:{2}] ", newPath, memberName, sourceLineNumber);
        }

        public static void Exception(Exception e, string? message = null,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Exception, memberName, sourceFilePath, sourceLineNumber);
            builder.Append(message ?? e.Message);
            builder.AppendLine($"{e}");
            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn(string message,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        public static void Error(string message,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug(string message,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info(string message,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info, memberName, sourceFilePath, sourceLineNumber);
            builder.Append(message);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1>(string format, T1 value,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2>(string format, T1 value, T2 value2,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1>(string format, T1 value,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2>(string format, T1 value, T2 value2,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1>(string format, T1 value,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2>(string format, T1 value, T2 value2,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1>(string format, T1 value,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2>(string format, T1 value, T2 value2,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }
    }
}
