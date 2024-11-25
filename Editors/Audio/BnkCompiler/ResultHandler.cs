using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using System.IO;

namespace Editors.Audio.BnkCompiler
{
    public class ResultHandler
    {
        private readonly IPackFileService _pfs;
        private readonly IFileSaveService _packFileSaveService;

        public ResultHandler(IPackFileService pfs, IFileSaveService packFileSaveService)
        {
            _pfs = pfs;
            _packFileSaveService = packFileSaveService;
        }

        internal Result<bool> ProcessResult(CompileResult compileResult, CompilerData compilerData, CompilerSettings settings)
        {
            SaveToPackFile(compileResult, compilerData, settings);
            ExportToDirectory(compileResult, settings);
            return Result<bool>.FromOk(true);
        }

        void SaveToPackFile(CompileResult compileResult, CompilerData compilerData, CompilerSettings settings)
        {
            var bnkOutputPath = "audio\\wwise";
            var datOutputPath = "audio\\wwise";
            if (string.IsNullOrWhiteSpace(compilerData.ProjectSettings.Language) == false)
                bnkOutputPath += $"\\{compilerData.ProjectSettings.Language}";

            _packFileSaveService.Save(bnkOutputPath, compileResult.OutputBnkFile.DataSource.ReadData(), true);

            if (compileResult.Project.Events.Count > 0)
                _packFileSaveService.Save(datOutputPath, compileResult.OutputDatFile.DataSource.ReadData(), true);

            if (compileResult.Project.DialogueEvents.Count > 0)
                _packFileSaveService.Save(datOutputPath, compileResult.OutputStatesDatFile.DataSource.ReadData(), true);
        }

        void ExportToDirectory(CompileResult result, CompilerSettings settings)
        {
            var outputDirectory = settings.FileExportPath;

            if (string.IsNullOrWhiteSpace(outputDirectory) == false)
            {
                var bnkPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.bnk");
                File.WriteAllBytes(bnkPath, result.OutputBnkFile.DataSource.ReadData());

                if (result.Project.Events.Count > 0)
                {
                    var datPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
                    File.WriteAllBytes(datPath, result.OutputDatFile.DataSource.ReadData());
                }

                if (result.Project.DialogueEvents.Count > 0)
                {
                    var statesDatPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
                    File.WriteAllBytes(statesDatPath, result.OutputStatesDatFile.DataSource.ReadData());
                }
            }
        }
    }

}
