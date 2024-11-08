using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.SystemConsole.Themes;
using Shared.Core.Misc;

namespace Shared.Core.ErrorHandling
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



    public class CustomSink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formater;
        private readonly LogEventLevel _logLevel;
        private readonly Queue<string> _queue = [];

        public CustomSink(LogEventLevel logLevel, string outputTemplate)
        {

            _formater = new MessageTemplateTextFormatter(outputTemplate, null);
            _logLevel = logLevel;
        }


        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level >= _logLevel)
            {
                if (_queue.Count == 30)
                    _queue.Dequeue();

                using var _textWriter = new StringWriter();
                _formater.Format(logEvent, _textWriter);
               
                _queue.Enqueue(_textWriter.ToString());
            }
        }

        public List<string> GetHistory() => _queue.ToList();

    }


    public class Logging
    {
        public static ILogger Create<T>() => Log.ForContext<T>();
        public static ILogger CreateStatic(Type type) => Log.ForContext(type);

        static bool IsConfigured = false;
        public static string LogName { get; private set; }

        public static CustomSink? CustomSink;
        public static void Configure(LogEventLevel logEventLevel)
        {
            if (IsConfigured)
                return;

            var logDirectory = DirectoryHelper.LogDirectory;
            var logDate = DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day;
            var fileName = logDirectory + "\\" + logDate + ".log";
            fileName = getNextFileName(fileName);
            LogName = fileName;
         
            var outputTemplate = "[{Timestamp:HH:mm:ss} {Level}] [{ThreadId}] {SourceContext}::{MemberName} : {Message} {Exception}{NewLine}";
            var formatter = new MessageTemplateTextFormatter(outputTemplate, null);
     

            CustomSink = new CustomSink(logEventLevel, outputTemplate);

            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .Enrich.FromLogContext()
                        .Enrich.WithThreadId()
                        .WriteTo.File(LogName, logEventLevel, outputTemplate)
                        .WriteTo.Console(logEventLevel, outputTemplate, theme: AnsiConsoleTheme.Literate)
                        .WriteTo.Sink(CustomSink)
                        .CreateLogger();

            IsConfigured = true;
        }

        private static string getNextFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            var i = 0;
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
