using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler
{
    using Audio.Utility;
    using Shared.Core.ErrorHandling;
    using Shared.Core.Misc;
    using Shared.Core.PackFiles;
    using static Audio.Utility.WWiseWavToWem;

    public class AudioFileImporter
    {
        private readonly PackFileService _pfs;
        private readonly VgStreamWrapper _vgStreamWrapper;

        public AudioFileImporter(PackFileService pfs, VgStreamWrapper vgStreamWrapper)
        {
            _pfs = pfs;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public Result<bool> ImportAudio(CompilerData compilerData)
        {
            List<string> wavFiles = new List<string>();
            List<string> wavFilePaths = new List<string>();

            foreach (var gameSound in compilerData.GameSounds)
            {
                var wavFile = Path.GetFileName(gameSound.SoundPath);
                wavFiles.Add(wavFile);
                wavFilePaths.Add(gameSound.SoundPath);
            }

            var wavToWem = new WWiseWavToWem();
            InitialiseWwiseProject();
            wavToWem.WavToWem(wavFiles, wavFilePaths);

            foreach (var gameSound in compilerData.GameSounds)
            {
                var converterResult = ImportFromDisk(compilerData, gameSound);
                if (converterResult.IsSuccess == false)
                    return converterResult;
            }
            return Result<bool>.FromOk(true);
        }

        private Result<bool> ImportFromDisk(CompilerData compilerData, GameSound gameSound)
        {
            if (File.Exists(gameSound.SoundPath) == false)
                return Result<bool>.FromError("Audio converter", $"Importing from disk: Unable to find file '{gameSound.SoundPath}' for item '{gameSound.Id}' on disk");

            // Convert file
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var audioFolderPath = $"{tempFolderPath}\\Audio";
            var wavFile = Path.GetFileName(gameSound.SoundPath);
            var wavFileName = wavFile.Replace(".wav", "");
            var wemFile = wavFile.Replace(".wav", ".wem");
            var wemPath = $"{audioFolderPath}\\{wemFile}";

            // Compute hash
            var hashName = WwiseHash.Compute(wavFileName);

            // Load
            var createdFiles = PackFileUtil.LoadFilesFromDisk(_pfs, new PackFileUtil.FileRef(wemPath, GetExpectedFolder(compilerData), $"{hashName}.wem"));
            gameSound.SoundPath = _pfs.GetFullPath(createdFiles.First());

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
