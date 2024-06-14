using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using System.IO;

namespace Editors.Audio.BnkCompiler
{
    public class ResultHandler
    {
        private readonly PackFileService _pfs;

        public ResultHandler(PackFileService pfs)
        {
            _pfs = pfs;
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

            SaveHelper.SavePackFile(_pfs, bnkOutputPath, compileResult.OutputBnkFile, true);

            if (compileResult.Project.Events.Count > 0)
                SaveHelper.SavePackFile(_pfs, datOutputPath, compileResult.OutputDatFile, true);

            if (compileResult.Project.DialogueEvents.Count > 0)
                SaveHelper.SavePackFile(_pfs, datOutputPath, compileResult.OutputStatesDatFile, true);
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
