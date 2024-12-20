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
            var language = compilerData.ProjectSettings.Language;
            var bnkOutputPath = string.IsNullOrWhiteSpace(language) ? $"audio\\wwise\\{compileResult.OutputBnkFile.Name}" : $"audio\\wwise\\{language}\\{compileResult.OutputBnkFile.Name}";
            _packFileSaveService.Save(bnkOutputPath, compileResult.OutputBnkFile.DataSource.ReadData(), true);

            if (compileResult.Project.Events.Count > 0)
            {
                var datOutputPath = $"audio\\wwise\\{compileResult.OutputEventDatFile.Name}";
                _packFileSaveService.Save(datOutputPath, compileResult.OutputEventDatFile.DataSource.ReadData(), true);
            }

            if (compileResult.Project.DialogueEvents.Count > 0)
            {
                var datOutputPath = $"audio\\wwise\\{compileResult.OutputStateDatFile.Name}";
                _packFileSaveService.Save(datOutputPath, compileResult.OutputStateDatFile.DataSource.ReadData(), true);
            }
        }

        private static void ExportToDirectory(CompileResult result, CompilerSettings settings)
        {
            var outputDirectory = settings.FileExportPath;

            if (string.IsNullOrWhiteSpace(outputDirectory) == false)
            {
                var bnkPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.bnk");
                File.WriteAllBytes(bnkPath, result.OutputBnkFile.DataSource.ReadData());

                if (result.Project.Events.Count > 0)
                {
                    var datPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
                    File.WriteAllBytes(datPath, result.OutputEventDatFile.DataSource.ReadData());
                }

                if (result.Project.DialogueEvents.Count > 0)
                {
                    var statesDatPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
                    File.WriteAllBytes(statesDatPath, result.OutputStateDatFile.DataSource.ReadData());
                }
            }
        }
    }

}
