using Audio.Utility;
using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using System.IO;

namespace Audio.BnkCompiler
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

            SaveHelper.SavePackFile(_pfs, bnkOutputPath, compileResult.OutputBnkFile, false);
            SaveHelper.SavePackFile(_pfs, datOutputPath, compileResult.OutputDatFile, false);
        }

        void ExportToDirectory(CompileResult result, CompilerSettings settings)
        {
            string outputDirectory = settings.FileExportPath;
            bool convertResultToXml = settings.ConvertResultToXml;

            if (string.IsNullOrWhiteSpace(outputDirectory) == false)
            {
                var bnkPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.bnk");
                File.WriteAllBytes(bnkPath, result.OutputBnkFile.DataSource.ReadData());

                var datPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
                File.WriteAllBytes(datPath, result.OutputDatFile.DataSource.ReadData());

                if (convertResultToXml)
                {
                    Guard.IsNotNullOrEmpty(settings.WWiserPath);
                    BnkToXmlConverter.Convert(settings.WWiserPath, bnkPath, true);
                }
            }
        }
    }

}
