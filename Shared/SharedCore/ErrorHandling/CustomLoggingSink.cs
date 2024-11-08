using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Shared.Core.ErrorHandling
{
    public class CustomLoggingSink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formater;
        private readonly LogEventLevel _logLevel;
        private readonly Queue<string> _queue = [];

        public CustomLoggingSink(LogEventLevel logLevel, string outputTemplate)
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
}
