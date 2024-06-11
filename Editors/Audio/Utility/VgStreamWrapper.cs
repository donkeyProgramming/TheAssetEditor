using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;

namespace Audio.Utility
{
    public class VgStreamWrapper
    {
        ILogger _logger = Logging.Create<VgStreamWrapper>();

        string VgStreamFolderName => $"{DirectoryHelper.Temp}\\VgStream";
        string AudioFolderName => $"{DirectoryHelper.Temp}\\Audio";
        void EnsureCreated() => GetCliPath();

        public VgStreamWrapper()
        {
            try
            {
                EnsureCreated();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Unable to create VgStreamWrapper: {e.Message}");
            }
        }

        public Result<string> ConvertFromWem(string fileNameWithoutExtention, byte[] wemBytes)
        {
            Guard.IsNotNull(wemBytes);
            Guard.IsNotNullOrEmpty(fileNameWithoutExtention);

            var wemName = $"{AudioFolderName}\\{fileNameWithoutExtention}.wem";
            var wavName = $"{AudioFolderName}\\{fileNameWithoutExtention}.wav";

            _logger.Here().Information($"Trying to export sound '{fileNameWithoutExtention}' - {wemBytes.Length} bytes");

            var exportResult = ExportFile(wemName, wemBytes);
            if (exportResult.Failed)
                return Result<string>.FromError(exportResult.LogItems);

            return ConvertFileUsingVgSteam(wemName, wavName);
        }

        private Result<string> ConvertFileUsingVgSteam(string sourceFileName, string targetFileName)
        {
            try
            {
                var cliPath = GetCliPath();
                _logger.Here().Information($"VgSteam path is '{cliPath}'");
                _logger.Here().Information($"Trying to convert {sourceFileName} to {targetFileName}");

                var arguments = $"-o \"{targetFileName}\" \"{sourceFileName}\"";
                _logger.Here().Information($"{cliPath} {arguments}");

                using var pProcess = new System.Diagnostics.Process();
                pProcess.StartInfo.FileName = cliPath;
                pProcess.StartInfo.Arguments = arguments;
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = false;
                pProcess.Start();
                var output = pProcess.StandardOutput.ReadToEnd();
                _logger.Here().Information(output);
                pProcess.WaitForExit();

                var outputSoundFilePath = targetFileName;
                var doesFileExist = File.Exists(outputSoundFilePath);
                _logger.Here().Information($"File readback result for converted file {outputSoundFilePath} is : {doesFileExist}");
                if (doesFileExist == false)
                    return Result<string>.FromError("VgSteam", $"Failed to convert file - File {outputSoundFilePath} no found on disk");
                return Result<string>.FromOk(outputSoundFilePath);
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                return Result<string>.FromError("Convert error", e.Message);
            }
        }

        Result<bool> ExportFile(string filePath, byte[] bytes)
        {
            try
            {
                DirectoryHelper.EnsureFileFolderCreated(filePath);
                File.WriteAllBytes(filePath, bytes);
                _logger.Here().Information("All bytes written to file");
                return Result<bool>.FromOk(true);
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                return Result<bool>.FromError("Write error", e.Message);
            }
        }

        string GetCliPath()
        {
            DirectoryHelper.EnsureCreated(VgStreamFolderName);
            DirectoryHelper.EnsureCreated(AudioFolderName);

            var vgStreamCli = $"{VgStreamFolderName}\\vgstream.exe";
            if (File.Exists(vgStreamCli))
                return vgStreamCli;

            var executingAssembly = Assembly.GetExecutingAssembly();
            string folderName = string.Format("{0}.Resources.VgStream.", executingAssembly.GetName().Name);

            var vgStreamFiles = executingAssembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            foreach (var file in vgStreamFiles)
            {
                var fileName = file.Replace(folderName, "", StringComparison.InvariantCultureIgnoreCase);
                var outputFileName = $"{VgStreamFolderName}\\{fileName}";
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
                using var fStream = new FileStream(outputFileName, FileMode.OpenOrCreate);
                stream!.CopyTo(fStream);
            }

            if (File.Exists(vgStreamCli) == false)
                throw new Exception("Failed to create vgStreamCli - Unknown error");

            return vgStreamCli;
        }
    }
}
