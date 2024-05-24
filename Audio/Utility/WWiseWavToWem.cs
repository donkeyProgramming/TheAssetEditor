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
        //void EnsureCreated() => GetCliPath();
        ApplicationSettingsService _settingsService;

        public WWiseWavToWem()
        {
            _settingsService = new ApplicationSettingsService();
        }

        public void CreateWsources(List<string> wavFiles, string audioFolderPath)
        {

            var wsourcesPath = $"{DirectoryHelper.Temp}\\wav_to_wem.wsources";
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

        public void CopyWavs(List<string> wavFilePaths)
        {
            foreach (var wavFilePath in wavFilePaths)
            {
                var originalWavPath = wavFilePath;
                var wavFile = Path.GetFileName(wavFilePath);
                var tempWavPath = $"{DirectoryHelper.Temp}\\Audio\\{wavFile}";
                try
                {
                    // Copy the file
                    File.Copy(originalWavPath, tempWavPath, true); // The 'true' parameter allows overwriting if the file already exists
                    Console.WriteLine($"Copied {wavFile} from {originalWavPath} to {tempWavPath}");
                }
                catch (IOException iox)
                {
                    Console.WriteLine($"Error occurred when coppying file: {iox.Message}");
                }
            }
        }

        public void WavToWem(List<string> wavFiles, List<string> wavFilePaths)
        {

            // Define the root path to Wwise and the specific WwiseCLI executable
            var wwiseCliPath = _settingsService.CurrentSettings.WwisePath;

            // Path to the parent directory of the current directory
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var audioFolderPath = $"{tempFolderPath}\\Audio";
            var wprojPath = $"{tempFolderPath}\\WavToWemWwiseProject\\WavToWemWwiseProject.wproj";
            var wsourcesPath = $"{tempFolderPath}\\wav_to_wem.wsources";

            CopyWavs(wavFilePaths);
            CreateWsources(wavFiles, audioFolderPath);

            // Construct the command arguments for WwiseCLI
            var arguments = $"{wprojPath} -ConvertExternalSources {wsourcesPath} -ExternalSourcesOutput {audioFolderPath}";
                
            // Run WwiseCLI.exe with the specified arguments
            RunExternalCommand(wwiseCliPath, arguments);
            DeleteExcessStuff(audioFolderPath);
        }

        void RunExternalCommand(string filePath, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    // Optionally, capture and display/log the output
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    Console.WriteLine(output);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.Error.WriteLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running command: {ex.Message}");
            }
        }

        public void DeleteExcessStuff(string audioFolderPath)
        {
            var excessFolderPath = $"{audioFolderPath}\\Windows";

            // Ensure the source directory exists
            if (Directory.Exists(excessFolderPath))
            {
                // Get all .wem files in the source directory
                string[] wemFiles = Directory.GetFiles(excessFolderPath, "*.wem");

                foreach (string file in wemFiles)
                {
                    // Get the filename
                    string fileName = Path.GetFileName(file);
                    // Define the destination path for the file
                    string destFile = Path.Combine(audioFolderPath, fileName);

                    // Check if the file already exists at the destination
                    if (!File.Exists(destFile))
                    {
                        // Move the file
                        File.Move(file, destFile);
                        Console.WriteLine($"Moved {file} to {destFile}");
                    }
                    else
                    {
                        Console.WriteLine($"File {fileName} already exists at the destination. Skipping move.");
                    }
                }

                // Get all remaining files in the source directory
                string[] remainingFiles = Directory.GetFiles(excessFolderPath);

                foreach (string file in remainingFiles)
                {
                    // Delete the file
                    File.Delete(file);
                    Console.WriteLine($"Deleted {file}");
                }

                // Attempt to delete the source directory
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

        public void InitialiseWwiseProject()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceRootNamespace = "Audio.Resources";
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var wavToWemWwiseProjectPath = $"{tempFolderPath}\\wavToWemWwiseProjectPath";

            // Check if the wavToWemWwiseProjectPath folder exists
            if (Directory.Exists(wavToWemWwiseProjectPath))
                return;

            else
            {
                // Construct the full namespace for the embedded resource
                var resourceFolderPath = $"{resourceRootNamespace}.WavToWemWwiseProject.WavToWemWwiseProject.zip";

                // Use a temporary file path to save the embedded zip before extracting
                var tempZipPath = Path.Combine(Path.GetTempPath(), "WavToWemWwiseProject.zip");

                // Extract the embedded resource and write it to the temporary file
                using (var resourceStream = assembly.GetManifestResourceStream(resourceFolderPath))
                {
                    if (resourceStream == null)
                        throw new InvalidOperationException($"Resource {resourceFolderPath} not found. Make sure the resource exists and is set to 'Embedded Resource'.");

                    using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write);
                    resourceStream.CopyTo(fileStream);
                }

                // Extract the zip file to the specified path
                ZipFile.ExtractToDirectory(tempZipPath, tempFolderPath, overwriteFiles: true);

                // Optionally, delete the temporary zip file after extraction
                File.Delete(tempZipPath);
            }
        }
    }
}
