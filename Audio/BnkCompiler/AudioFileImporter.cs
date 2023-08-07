using Audio.Utility;
using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler
{
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
            foreach (var gameSound in compilerData.GameSounds)
            {
                var importType = DetermineImportType(compilerData, gameSound);
                if (importType == SoundFileImportType.None)
                {
                    continue;
                }
                else if (importType == SoundFileImportType.Unknown)
                {
                    return Result<bool>.FromError("Audio converter", $"Unable to determine import type for '{gameSound.Path}' for item '{gameSound.Name}'");
                }
                else if (importType == SoundFileImportType.Disk)
                {
                    var converterResult = ImportFromDisk(compilerData, gameSound);
                    if (converterResult.IsSuccess == false)
                        return converterResult;
                }
                else if (importType == SoundFileImportType.PackFile)
                {
                    var converterResult = ImportFromPackFile(compilerData, gameSound);
                    if (converterResult.IsSuccess == false)
                        return converterResult;
                }
            }

            // Sanity check
            foreach (var gameSound in compilerData.GameSounds)
            {
                Guard.IsNotNullOrWhiteSpace(gameSound.Path);
                Guard.IsNotNull(_pfs.FindFile(gameSound.Path));
            }

            return Result<bool>.FromOk(true);
        }

        private Result<bool> ImportFromPackFile(CompilerData compilerData, GameSound gameSound)
        {
            var file = _pfs.FindFile(gameSound.Path);
            if (file == null)
                return Result<bool>.FromError("Audio converter", $"Importing from packfile: Unable to find file '{gameSound.Path}' for item '{gameSound.Name}' on disk");

            // Convert file
            var wemPath = _vgStreamWrapper.ConvertToWem(file);
            if (wemPath.Failed)
                return Result<bool>.FromError(wemPath.LogItems);

            // Compute hash
            var fileName = Path.GetFileName(wemPath.Item);
            var hashName = WWiseHash.Compute30(fileName);

            // Load
            var createdFiles = PackFileUtil.LoadFilesFromDisk(_pfs, new PackFileUtil.FileRef(wemPath.Item, GetExpectedFolder(compilerData), $"{hashName}.wem"));
            gameSound.Path = _pfs.GetFullPath(createdFiles.First());

            return Result<bool>.FromOk(true);
        }

        private Result<bool> ImportFromDisk(CompilerData compilerData, GameSound gameSound)
        {
            if (File.Exists(gameSound.Path) == false)
                return Result<bool>.FromError("Audio converter", $"Importing from disk: Unable to find file '{gameSound.Path}' for item '{gameSound.Name}' on disk");

            // Convert file
            var wemPath = _vgStreamWrapper.ConvertToWem(gameSound.Path);
            if (wemPath.Failed)
                return Result<bool>.FromError(wemPath.LogItems);

            // Compute hash
            var fileName = Path.GetFileName(wemPath.Item);
            var hashName = WWiseHash.Compute30(fileName);

            // Load
            var createdFiles = PackFileUtil.LoadFilesFromDisk(_pfs, new PackFileUtil.FileRef(wemPath.Item, GetExpectedFolder(compilerData), $"{hashName}.wem"));
            gameSound.Path = _pfs.GetFullPath(createdFiles.First());

            return Result<bool>.FromOk(true);
        }

        SoundFileImportType DetermineImportType(CompilerData compilerData, GameSound gameSound)
        {
            var path = gameSound.Path;
            if (File.Exists(path))
                return SoundFileImportType.Disk;

            if (_pfs.FindFile(path) != null)
            {
                var filename = Path.GetFileNameWithoutExtension(path);

                // Check if file has correct naming
                var convertResult = uint.TryParse(filename, out var _);
                if (convertResult == false)
                    return SoundFileImportType.PackFile;

                // Check if file has correct extension
                var extension = Path.GetExtension(path).ToLower();
                if (extension != ".wem")
                    return SoundFileImportType.PackFile;

                // Check if file is in correct folder
                var filePath = Path.GetDirectoryName(path);
                var expectedFolder = GetExpectedFolder(compilerData);
                if (string.Compare(filePath, expectedFolder, true) != 0)
                    return SoundFileImportType.PackFile;

                return SoundFileImportType.None;
            }

            return SoundFileImportType.Unknown;
        }

        string GetExpectedFolder(CompilerData compilerData)
        {
            var basePath = "audio\\wwise";
            if (string.IsNullOrEmpty(compilerData.ProjectSettings.Language) == false)
                basePath += $"\\{compilerData.ProjectSettings.Language}";

            return basePath;
        }



        enum SoundFileImportType
        {
            None,
            Unknown,
            Disk,
            PackFile
        }

    }
}
