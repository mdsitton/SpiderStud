using System;
using System.Diagnostics;
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
        public static void FormatPrefix(LogLevel level)
        {
            builder.AppendFormat("{0} [{1}] ", DateTime.Now, level);
        }

        public static void Exception(Exception e, string? message)
        {

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Exception);
            builder.Append(message ?? e.Message);
            builder.AppendLine($"{e}");
            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn(string message)
        {
            if (Level > LogLevel.Warn)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        public static void Error(string message)
        {
            if (Level > LogLevel.Error)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            if (Level > LogLevel.Debug)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug);
            builder.Append(message);
            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info(string message)
        {
            if (Level > LogLevel.Info)
                return;

            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info);
            builder.Append(message);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1>(string format, T1 value)
        {
            if (Level > LogLevel.Info)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2>(string format, T1 value, T2 value2)
        {
            if (Level > LogLevel.Info)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3)
        {
            if (Level > LogLevel.Info)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Info<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4)
        {
            if (Level > LogLevel.Info)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Info);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1>(string format, T1 value)
        {
            if (Level > LogLevel.Debug)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2>(string format, T1 value, T2 value2)
        {
            if (Level > LogLevel.Debug)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3)
        {
            if (Level > LogLevel.Debug)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4)
        {
            if (Level > LogLevel.Debug)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Debug);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1>(string format, T1 value)
        {
            if (Level > LogLevel.Warn)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2>(string format, T1 value, T2 value2)
        {
            if (Level > LogLevel.Warn)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3)
        {
            if (Level > LogLevel.Warn)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Warn<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4)
        {
            if (Level > LogLevel.Warn)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Warn);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1>(string format, T1 value)
        {
            if (Level > LogLevel.Error)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error);
            builder.AppendFormat(format, value);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2>(string format, T1 value, T2 value2)
        {
            if (Level > LogLevel.Error)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error);
            builder.AppendFormat(format, value, value2);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2, T3>(string format, T1 value, T2 value2, T3 value3)
        {
            if (Level > LogLevel.Error)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error);
            builder.AppendFormat(format, value, value2, value3);

            LogAction?.Invoke(builder.ToString());
        }

        public static void Error<T1, T2, T3, T4>(string format, T1 value, T2 value2, T3 value3, T4 value4)
        {
            if (Level > LogLevel.Error)
                return;


            if (builder == null)
                builder = new StringBuilder();

            builder.Clear();
            FormatPrefix(LogLevel.Error);
            builder.AppendFormat(format, value, value2, value3, value4);

            LogAction?.Invoke(builder.ToString());
        }
    }
}
