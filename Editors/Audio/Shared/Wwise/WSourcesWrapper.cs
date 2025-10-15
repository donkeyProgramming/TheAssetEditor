using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.Settings;


namespace Editors.Audio.Shared.Wwise
{
    public class WSourcesWrapper
    {
        private readonly ApplicationSettingsService _applicationSettingsService;

        private readonly ILogger _logger = Logging.Create<WSourcesWrapper>();

        private readonly string _wavToWemFolderPath = $"{DirectoryHelper.Temp}\\WavToWem";
        private readonly string _audioFolderPath = $"{DirectoryHelper.Temp}\\Audio";
        private readonly string _excessFilesFolderPath = $"{DirectoryHelper.Temp}\\Audio\\Windows";
        private readonly string _wsourcesPath = $"{DirectoryHelper.Temp}\\WavToWem\\wav_to_wem.wsources";
        private readonly string _wwiseCliPath;

        public WSourcesWrapper(ApplicationSettingsService applicationSettingsService)
        {
            _applicationSettingsService = applicationSettingsService;
            _wwiseCliPath = _applicationSettingsService.CurrentSettings.WwisePath;
        }

        public void CreateWsourcesFile(List<string> wavFilesNames)
        {
            var sources = from wavFileName in wavFilesNames 
                          select new XElement("Source", new XAttribute("Path", wavFileName), new XAttribute("Conversion", "Vorbis Quality High"));

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null), 
                new XElement("ExternalSourcesList", 
                new XAttribute("SchemaVersion", "1"), 
                new XAttribute("Root", _audioFolderPath), 
                sources));

            document.Save(_wsourcesPath);

            _logger.Here().Information($"Saved WSources file to {_wsourcesPath}");
        }

        public void RunExternalCommand(string arguments)
        {
            _logger.Here().Information($"Running WwiseCLI.exe with arguments {arguments}");
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

                _logger.Here().Information($"{output}");
                if (!string.IsNullOrEmpty(error))
                    _logger.Here().Error($"{error}");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error running command: {e.Message}");
            }
        }

        public void DeleteExcessStuff()
        {
            if (Directory.Exists(_excessFilesFolderPath))
            {
                var wemFiles = Directory.GetFiles(_excessFilesFolderPath, "*.wem");
                foreach (var file in wemFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileDestination = Path.Combine(_audioFolderPath, fileName);

                    if (!File.Exists(fileDestination))
                        File.Move(file, fileDestination);
                }

                var remainingFiles = Directory.GetFiles(_excessFilesFolderPath);
                foreach (var file in remainingFiles)
                    File.Delete(file);

                try
                {
                    Directory.Delete(_excessFilesFolderPath, true);
                }
                catch (IOException e)
                {
                    throw new InvalidOperationException($"The directory could not be deleted or another error occurred: {e.Message}");
                }
            }
            else
            {
                _logger.Here().Information($"The directory {_excessFilesFolderPath} does not exist.");
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

            // TODO: Update this for game abstraction, currently it's set to the WH3 Wwise Project resource but for future games will need abstraction.
            var resourceFolderPath = $"{resourceRootNamespace}.Wh3WavToWemWwiseProject.Wh3WavToWemWwiseProject.zip";
            var tempZipPath = Path.Combine(Path.GetTempPath(), "Wh3WavToWemWwiseProject.zip");

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
