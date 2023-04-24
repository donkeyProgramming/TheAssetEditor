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

    public class CompilerSettings
    {
        public bool UserOerrideIdForActions { get; set; } = false;
        public bool UseOverrideIdForMixers { get; set; } = false;
        public bool UseOverrideIdForSounds { get; set; } = false;

        public string WWiserPath { get; set; } = "D:\\Research\\Audio\\WWiser\\wwiser.pyz";
        public bool ConvertResultToXml { get; set; }
        public string FileExportPath { get; set; }
    }

    public class CompilerService
    {
        ILogger _logger = Logging.Create<CompilerConsoleLogger>();
        ICompilerLogger _errorLogger;

        ProjectLoader _loader;
        Compiler _compiler;
        ResultHandler _resultHandler;

        public CompilerService(ICompilerLogger logger, ProjectLoader loader, Compiler compiler, ResultHandler resultHandler)
        {
            _errorLogger = logger;
            _loader = loader;
            _compiler = compiler;
            _resultHandler = resultHandler;
        }

        public bool Compile(string packFilePath, CompilerSettings settings) 
        {
            var loadResult = _loader.LoadProject(packFilePath, settings);
            if (loadResult.Success == false)
            {
                _errorLogger.Log(loadResult.ErrorList);
                return false;
            }

            // Generate sounds if needed

            var compileResult = _compiler.CompileProject(loadResult.Item);
            if(compileResult.Success == false) 
            {
                _errorLogger.Log(compileResult.ErrorList);
                return false;
            }

            var handlerResult = _resultHandler.ProcessResult(compileResult.Item, settings);
            if (handlerResult.Success == false) 
            {
                _errorLogger.Log(handlerResult.ErrorList);
                return false;
            }

            return true;
        }
    }
}
