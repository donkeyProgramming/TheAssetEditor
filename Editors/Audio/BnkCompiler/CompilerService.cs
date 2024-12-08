﻿using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.BnkCompiler
{
    public class CompilerSettings
    {
        public bool UserIdForActions { get; set; } = false;
        public bool UseIdForMixers { get; set; } = false;
        public bool UseIdForSounds { get; set; } = false;
        public bool SaveGeneratedCompilerInput { get; set; } = true;
        public string FileExportPath { get; set; }

        public static CompilerSettings Default() => new CompilerSettings();
    }

    public class CompilerService
    {
        readonly ILogger _logger = Logging.Create<CompilerService>();

        readonly ProjectLoader _loader;
        readonly Compiler _compiler;
        readonly ResultHandler _resultHandler;
        readonly AudioFileImporter _audioFileImporter;

        public CompilerService(ProjectLoader loader, AudioFileImporter audioFileImporter, Compiler compiler, ResultHandler resultHandler)
        {
            _loader = loader;
            _audioFileImporter = audioFileImporter;
            _compiler = compiler;
            _resultHandler = resultHandler;
        }

        public Result<bool> Compile(string packFilePath, CompilerSettings settings)
        {
            var project = _loader.LoadProject(packFilePath, settings);
            if (project.IsSuccess == false)
                return Result<bool>.FromError(project.LogItems);

            var importResult = _audioFileImporter.ImportAudio(project.Item);
            if (importResult.IsSuccess == false)
                return Result<bool>.FromError(importResult.LogItems);

            var compilerOutput = _compiler.CompileProject(project.Item);
            if (compilerOutput.IsSuccess == false)
                return Result<bool>.FromError(compilerOutput.LogItems);

            var handlerResult = _resultHandler.ProcessResult(compilerOutput.Item, project.Item, settings);
            if (handlerResult.IsSuccess == false)
                return Result<bool>.FromError(handlerResult.LogItems);

            _logger.Here().Information("Bnk file generated successfully");
            return Result<bool>.FromOk(true);
        }
    }
}
