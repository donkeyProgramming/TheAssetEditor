using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Shared.Core.Misc;
using Shared.Core.Services;

namespace Audio.Utility
{
    public class WWiseWavToWem
    {
        readonly ApplicationSettingsService _settingsService;

        public WWiseWavToWem()
        {
            _settingsService = new ApplicationSettingsService();
        }

        public void WavToWem(List<string> wavFiles, List<string> wavFilePaths)
        {
            var wwiseCliPath = _settingsService.CurrentSettings.WwisePath; // Define the root path to Wwise and the specific WwiseCLI executable

            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var wavToWemFolderPath = $"{tempFolderPath}\\WavToWem";
            var audioFolderPath = $"{tempFolderPath}\\Audio";
            var wprojPath = $"{wavToWemFolderPath}\\WavToWemWwiseProject\\WavToWemWwiseProject.wproj";
            var wsourcesPath = $"{wavToWemFolderPath}\\wav_to_wem.wsources";

            DirectoryHelper.EnsureCreated(wavToWemFolderPath);

            CopyWavs(wavFilePaths);
            CreateWsources(wavFiles, audioFolderPath);

            var arguments = $"{wprojPath} -ConvertExternalSources {wsourcesPath} -ExternalSourcesOutput {audioFolderPath}"; 

            RunExternalCommand(wwiseCliPath, arguments);
            DeleteExcessStuff(audioFolderPath);
        }

        public static void CopyWavs(List<string> wavFilePaths)
        {
            foreach (var wavFilePath in wavFilePaths)
            {
                var originalWavPath = wavFilePath;
                var wavFile = Path.GetFileName(wavFilePath);
                var tempWavPath = $"{DirectoryHelper.Temp}\\Audio\\{wavFile}";
                try
                {
                    File.Copy(originalWavPath, tempWavPath, true); // The 'true' parameter allows overwriting if the file already exists
                    Console.WriteLine($"Copied {wavFile} from {originalWavPath} to {tempWavPath}");
                }
                catch (IOException iox)
                {
                    Console.WriteLine($"Error occurred when coppying file: {iox.Message}");
                }
            }
        }

        public static void CreateWsources(List<string> wavFiles, string audioFolderPath)
        {
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var wavToWemFolderPath = $"{tempFolderPath}\\WavToWem";
            var wsourcesPath = $"{wavToWemFolderPath}\\wav_to_wem.wsources";

            var sources = from wavFile in wavFiles
                          select new XElement("Source",
                              new XAttribute("Path", Path.GetFileName(wavFile)),
                              new XAttribute("Conversion", "Vorbis Quality High"));

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("ExternalSourcesList",
                    new XAttribute("SchemaVersion", "1"),
                    new XAttribute("Root", audioFolderPath),
                    sources));

            document.Save(wsourcesPath);

            Console.WriteLine($"Saved list.wsources to {wsourcesPath}");
        }

        static void RunExternalCommand(string filePath, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                Console.WriteLine(output);
                if (!string.IsNullOrEmpty(error))
                {
                    Console.Error.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running command: {ex.Message}");
            }
        }

        public static void DeleteExcessStuff(string audioFolderPath)
        {
            var excessFolderPath = $"{audioFolderPath}\\Windows";

            if (Directory.Exists(excessFolderPath))
            {
                var wemFiles = Directory.GetFiles(excessFolderPath, "*.wem");

                foreach (var file in wemFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileDestination = Path.Combine(audioFolderPath, fileName);

                    if (!File.Exists(fileDestination))
                    {
                        File.Move(file, fileDestination);
                        Console.WriteLine($"Moved {file} to {fileDestination}");
                    }
                    else
                        Console.WriteLine($"File {fileName} already exists at the destination. Skipping move.");
                }

                var remainingFiles = Directory.GetFiles(excessFolderPath);

                foreach (var file in remainingFiles)
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted {file}");
                }

                try
                {
                    Directory.Delete(excessFolderPath, true); // The true parameter allows for recursive deletion
                    Console.WriteLine($"Deleted directory {excessFolderPath}");
                }
                catch (IOException e)
                {
                    Console.WriteLine($"The directory could not be deleted or another error occurred: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine($"The directory {excessFolderPath} does not exist.");
            }
        }

        public static void InitialiseWwiseProject()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceRootNamespace = "Shared.Resources.AudioConversion";
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var wavToWemFolderPath = $"{tempFolderPath}\\WavToWem";
            var wavToWemWwiseProjectPath = $"{wavToWemFolderPath}\\WavToWemWwiseProjectPath";

            if (Directory.Exists(wavToWemWwiseProjectPath))
                return;

            else
            {
                var resourceFolderPath = $"{resourceRootNamespace}.WavToWemWwiseProject.WavToWemWwiseProject.zip";

                var tempZipPath = Path.Combine(Path.GetTempPath(), "WavToWemWwiseProject.zip");

                using (var resourceStream = assembly.GetManifestResourceStream(resourceFolderPath))
                {
                    if (resourceStream == null)
                        throw new InvalidOperationException($"Resource {resourceFolderPath} not found. Make sure the resource exists and is set to 'Embedded Resource'.");

                    using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write);
                    resourceStream.CopyTo(fileStream);
                }

                ZipFile.ExtractToDirectory(tempZipPath, wavToWemFolderPath, overwriteFiles: true);

                File.Delete(tempZipPath);
            }
        }
    }
}
