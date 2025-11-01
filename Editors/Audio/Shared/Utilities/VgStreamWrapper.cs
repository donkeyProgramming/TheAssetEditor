using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;

namespace Editors.Audio.Shared.Utilities
{
    public class VgStreamWrapper
    {
        readonly ILogger _logger = Logging.Create<VgStreamWrapper>();

        private static string VgStreamFolder => $"{DirectoryHelper.Temp}\\VgStream";
        private static string AudioFolder => $"{DirectoryHelper.Temp}\\Audio";
        static void EnsureCreated() => GetCliPath();

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

        public Result<string> ConvertFileUsingVgStream(string sourceFileName, string targetFileName)
        {
            try
            {
                var cliPath = GetCliPath();
                _logger.Here().Information($"VgSteam path is '{cliPath}'");
                _logger.Here().Information($"Trying to convert {sourceFileName} to {targetFileName}");

                var arguments = $"-o \"{targetFileName}\" \"{sourceFileName}\"";
                _logger.Here().Information($"{cliPath} {arguments}");

                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = cliPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = false;
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                _logger.Here().Information(output);
                process.WaitForExit();

                var outputSoundFilePath = targetFileName;
                var doesFileExist = File.Exists(outputSoundFilePath);
                _logger.Here().Information($"File readback result for converted file {outputSoundFilePath} is : {doesFileExist}");
                if (doesFileExist == false)
                    return Result<string>.FromError("VgSteam", $"Failed to convert file - File {outputSoundFilePath} not found on disk");
                return Result<string>.FromOk(outputSoundFilePath);
            }

            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                return Result<string>.FromError("Convert error", e.Message);
            }
        }

        private static string GetCliPath()
        {
            DirectoryHelper.EnsureCreated(VgStreamFolder);
            DirectoryHelper.EnsureCreated(AudioFolder);

            var vgStreamCli = Path.Combine(VgStreamFolder, "vgstream.exe");
            if (File.Exists(vgStreamCli))
                return vgStreamCli;

            var assemblyName = "Shared.EmbeddedResources";
            var assembly = Assembly.Load(assemblyName);
            var resourceRootNamespace = $"{assemblyName}.Resources.AudioConversion.vgstream";
            var vgStreamFiles = assembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(resourceRootNamespace, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            foreach (var file in vgStreamFiles)
            {
                var fileName = file.Substring(resourceRootNamespace.Length + 1);
                var outputFileName = $"{VgStreamFolder}\\{fileName}";

                using var resourceStream = assembly.GetManifestResourceStream(file);
                if (resourceStream == null)
                    throw new Exception($"Failed to retrieve resource stream for {file}");

                using var fileStream = new FileStream(outputFileName, FileMode.OpenOrCreate);
                resourceStream.CopyTo(fileStream);
            }

            return vgStreamCli;
        }
    }
}
