using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Common
{
    public static class LoggerExtensions
    {
        public static ILogger Here(this ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return logger
                .ForContext("MemberName", memberName)
                .ForContext("FilePath", sourceFilePath)
                .ForContext("LineNumber", sourceLineNumber);
        }
    }

    public class Logging
    {
        public static ILogger Create<T>() => Log.ForContext<T>();
        public static ILogger CreateStatic(Type type) => Log.ForContext(type);

        static bool IsConfigure = false;
        public static string LogName { get; private set; }
        public static void Configure(LogEventLevel logEventLevel)
        {
            if (IsConfigure)
                return;

            var logDirectory = DirectoryHelper.LogDirectory;
            var logDate = DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day;
            var fileName = logDirectory + "\\" + logDate + ".log";
            fileName = getNextFileName(fileName);
            LogName = fileName;

            var outputTemplate = "[{Timestamp:HH:mm:ss} {Level}] [{ThreadId}] {SourceContext}::{MemberName} : {Message} {Exception}{NewLine}";

            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .Enrich.FromLogContext()
                        .Enrich.WithThreadId()
                        .WriteTo.File(LogName, logEventLevel, outputTemplate)
                        .WriteTo.Console(logEventLevel, outputTemplate, theme: AnsiConsoleTheme.Literate)
                        .CreateLogger();

            IsConfigure = true;
        }

        private static string getNextFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            int i = 0;
            while (File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);
            }

            return fileName;
        }
    }
}
