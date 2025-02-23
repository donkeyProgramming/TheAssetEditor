using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.Utility;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.BnkCompiler
{
    public class AudioFileImporter
    {
        private readonly IPackFileService _packFileService;
        private readonly VgStreamWrapper _vgStreamWrapper;

        public AudioFileImporter(IPackFileService pfs, VgStreamWrapper vgStreamWrapper)
        {
            _packFileService = pfs;
            _vgStreamWrapper = vgStreamWrapper;
        }

        /*
        public Result<bool> ImportAudio(CompilerData compilerData)
        {
            InitialiseWwiseProject();

            var wavFilesNames = new List<string>();
            var wavFilePaths = new List<string>();

            foreach (var sound in compilerData.Sounds)
            {
                var wavFileName = Path.GetFileName(sound.FilePath);
                wavFilesNames.Add(wavFileName);
                wavFilePaths.Add(sound.FilePath);
            }

            var wavToWem = new WemGenerator();
            wavToWem.ConvertWavsToWems(wavFilesNames, wavFilePaths);

            foreach (var sound in compilerData.Sounds)
            {
                var converterResult = ImportFromDisk(compilerData, sound);
                if (converterResult.IsSuccess == false)
                    return converterResult;
            }
            return Result<bool>.FromOk(true);
        }
        */

        private Result<bool> ImportFromDisk(CompilerData compilerData, Sound sound)
        {
            if (File.Exists(sound.FilePath) == false)
                return Result<bool>.FromError("Audio converter", $"Importing from disk: Unable to find file '{sound.FilePath}' for item '{sound.Id}' on disk");

            // Convert file
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var audioFolderPath = $"{tempFolderPath}\\Audio";
            var wavFile = Path.GetFileName(sound.FilePath);
            var wavFileName = wavFile.Replace(".wav", "");
            var wemFile = wavFile.Replace(".wav", ".wem");
            var wemPath = $"{audioFolderPath}\\{wemFile}";

            // Compute hash
            var hashName = WwiseHash.Compute(wavFileName);

            // Load
            var createdFiles = PackFileUtil.LoadFileFromDisk(_packFileService, new PackFileUtil.FileRef(wemPath, GetExpectedFolder(compilerData), $"{hashName}.wem"));
            sound.FilePath = _packFileService.GetFullPath(createdFiles.First());

            return Result<bool>.FromOk(true);
        }

        static string GetExpectedFolder(CompilerData compilerData)
        {
            var basePath = "audio\\wwise";
            if (string.IsNullOrEmpty(compilerData.ProjectSettings.Language) == false)
                basePath += $"\\{compilerData.ProjectSettings.Language}";

            return basePath;
        }
    }
}
