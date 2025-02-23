using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Shared.Core.Misc;
using Shared.Core.Settings;

namespace Editors.Audio.Utility
{
    public class WemGenerator
    {
        private readonly ApplicationSettingsService _applicationSettingsService;

        private readonly string _wavToWemFolderPath = $"{DirectoryHelper.Temp}\\WavToWem";
        private readonly string _audioFolderPath = $"{DirectoryHelper.Temp}\\Audio";
        private readonly string _excessFilesFolderPath = $"{DirectoryHelper.Temp}\\Audio\\Windows";
        private readonly string _wprojPath = $"{DirectoryHelper.Temp}\\WavToWem\\WavToWemWwiseProject\\WavToWemWwiseProject.wproj";
        private readonly string _wsourcesPath = $"{DirectoryHelper.Temp}\\WavToWem\\wav_to_wem.wsources";
        private readonly string _wwiseCliPath;

        public WemGenerator(ApplicationSettingsService applicationSettingsService)
        {
            _applicationSettingsService = applicationSettingsService;
            _wwiseCliPath = _applicationSettingsService.CurrentSettings.WwisePath;
        }

        public void GenerateWems(List<Sound> audioProjectSounds)
        {
            InitialiseWwiseProject();

            var wavFileNames = audioProjectSounds
                .Select(sound => $"{sound.SourceID}.wav")
                .ToList();

            DirectoryHelper.EnsureCreated(_wavToWemFolderPath);

            CreateWsourcesFile(wavFileNames);

            var arguments = $"\"{_wprojPath}\" -ConvertExternalSources \"{_wsourcesPath}\" -ExternalSourcesOutput \"{_audioFolderPath}\"";

            RunExternalCommand(arguments);
            DeleteExcessStuff();
        }

        private void CreateWsourcesFile(List<string> wavFilesNames)
        {
            var sources = from wavFileName in wavFilesNames
                          select new XElement("Source",
                              new XAttribute("Path", wavFileName),
                              new XAttribute("Conversion", "Vorbis Quality High"));

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("ExternalSourcesList",
                    new XAttribute("SchemaVersion", "1"),
                    new XAttribute("Root", _audioFolderPath),
                    sources));

            document.Save(_wsourcesPath);

            Console.WriteLine($"Saved WSources file to {_wsourcesPath}");
        }

        private void RunExternalCommand(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = _wwiseCliPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = false,
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

        private void DeleteExcessStuff()
        {
            if (Directory.Exists(_excessFilesFolderPath))
            {
                var wemFiles = Directory.GetFiles(_excessFilesFolderPath, "*.wem");

                foreach (var file in wemFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileDestination = Path.Combine(_audioFolderPath, fileName);

                    if (!File.Exists(fileDestination))
                    {
                        File.Move(file, fileDestination);
                        Console.WriteLine($"Moved {file} to {fileDestination}");
                    }
                    else
                        Console.WriteLine($"File {fileName} already exists at the destination. Skipping move.");
                }

                var remainingFiles = Directory.GetFiles(_excessFilesFolderPath);

                foreach (var file in remainingFiles)
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted {file}");
                }

                try
                {
                    Directory.Delete(_excessFilesFolderPath, true); // The true parameter allows for recursive deletion
                    Console.WriteLine($"Deleted directory {_excessFilesFolderPath}");
                }
                catch (IOException e)
                {
                    Console.WriteLine($"The directory could not be deleted or another error occurred: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine($"The directory {_excessFilesFolderPath} does not exist.");
            }
        }

        public void InitialiseWwiseProject()
        {
            var assemblyName = "Shared.EmbeddedResources";
            var assembly = Assembly.Load(assemblyName);
            var resourceRootNamespace = $"{assemblyName}.Resources.AudioConversion";
            var wavToWemWwiseProjectPath = Path.Combine(_wavToWemFolderPath, "WavToWemWwiseProjectPath");

            if (Directory.Exists(wavToWemWwiseProjectPath))
                return;

            var resourceFolderPath = $"{resourceRootNamespace}.WavToWemWwiseProject.WavToWemWwiseProject.zip";
            var tempZipPath = Path.Combine(Path.GetTempPath(), "WavToWemWwiseProject.zip");

            using (var resourceStream = assembly.GetManifestResourceStream(resourceFolderPath))
            {
                if (resourceStream == null)
                    throw new InvalidOperationException($"Failed to retrieve resource stream for {resourceFolderPath}.");

                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write);
                resourceStream.CopyTo(fileStream);
            }

            ZipFile.ExtractToDirectory(tempZipPath, _wavToWemFolderPath, overwriteFiles: true);
            File.Delete(tempZipPath);
        }

    }
}
