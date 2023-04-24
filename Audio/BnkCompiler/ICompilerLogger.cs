using CommonControls.Common;
using Serilog;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace Audio.BnkCompiler
{
    public interface ICompilerLogger
    {
        public void Log(ErrorList errorList);
    }

    public class CompilerConsoleLogger : ICompilerLogger
    {
        ILogger _logger = Logging.Create<CompilerConsoleLogger>();
        public void Log(ErrorList errorList)
        {
            foreach (var item in errorList.Errors)
                _logger.Error(item.PrettyString);
        }
    }
}
