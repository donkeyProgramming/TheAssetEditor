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

        public static CompilerSettings Default() => new CompilerSettings();
    }

    public class CompilerService
    {
        ILogger _logger = Logging.Create<CompilerService>();

        ProjectLoader _loader;
        Compiler _compiler;
        ResultHandler _resultHandler;
        WemFileImporter _wemFileImporter;

        public CompilerService(ProjectLoader loader, WemFileImporter wemFileImporter, Compiler compiler, ResultHandler resultHandler)
        {
            _loader = loader;
            _wemFileImporter = wemFileImporter;
            _compiler = compiler;
            _resultHandler = resultHandler;
        }

        public Result<bool> Compile(string packFilePath, CompilerSettings settings) 
        {
            var project = _loader.LoadProject(packFilePath, settings);
            if (project.Success == false)
                return Result<bool>.FromError(project.LogItems);

            var importResult = _wemFileImporter.ImportAudio(project.Item);
            if (importResult.Success == false)
                return Result<bool>.FromError(importResult.LogItems);

            var compilerOutput = _compiler.CompileProject(project.Item);
            if(compilerOutput.Success == false) 
                return Result<bool>.FromError(compilerOutput.LogItems);

            var handlerResult = _resultHandler.ProcessResult(compilerOutput.Item, project.Item, settings);
            if (handlerResult.Success == false) 
                return Result<bool>.FromError(handlerResult.LogItems);

            _logger.Here().Information("Bnk file generated successfully");
            return Result<bool>.FromOk(true);
        }
    }
}
