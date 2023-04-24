using CommonControls.Common;
using Serilog;

namespace Audio.BnkCompiler
{
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
        WemFileImporter _wemFileImporter;

        public CompilerService(ICompilerLogger logger, ProjectLoader loader, WemFileImporter wemFileImporter, Compiler compiler, ResultHandler resultHandler)
        {
            _errorLogger = logger;
            _loader = loader;
            _wemFileImporter = wemFileImporter;
            _compiler = compiler;
            _resultHandler = resultHandler;
        }

        public bool Compile(string packFilePath, CompilerSettings settings) 
        {
            var project = _loader.LoadProject(packFilePath, settings);
            if (project.Success == false)
            {
                _errorLogger.Log(project.ErrorList);
                return false;
            }

            var importResult = _wemFileImporter.ImportAudio(project.Item);
            if (importResult.Success == false)
            {
                _errorLogger.Log(importResult.ErrorList);
                return false;
            }

            var compilerOutput = _compiler.CompileProject(project.Item);
            if(compilerOutput.Success == false) 
            {
                _errorLogger.Log(compilerOutput.ErrorList);
                return false;
            }

            var handlerResult = _resultHandler.ProcessResult(compilerOutput.Item, project.Item, settings);
            if (handlerResult.Success == false) 
            {
                _errorLogger.Log(handlerResult.ErrorList);
                return false;
            }

            return true;
        }
    }
}
