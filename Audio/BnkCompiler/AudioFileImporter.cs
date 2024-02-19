using Audio.Utility;
using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler
{
    using Audio.Utility;
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
                var wavFile = Path.GetFileName(gameSound.Path);
                wavFiles.Add(wavFile);
                wavFilePaths.Add(gameSound.Path);
            }

            var wavToWem = new WWiseWavToWem();
            wavToWem.InitialiseWwiseProject();
            wavToWem.WavToWem(wavFiles, wavFilePaths);

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
            }
            return Result<bool>.FromOk(true);
        }

        private Result<bool> ImportFromDisk(CompilerData compilerData, GameSound gameSound)
        {
            if (File.Exists(gameSound.Path) == false)
                return Result<bool>.FromError("Audio converter", $"Importing from disk: Unable to find file '{gameSound.Path}' for item '{gameSound.Name}' on disk");

            // Convert file
            var tempFolderPath = $"{DirectoryHelper.Temp}";
            var audioFolderPath = $"{tempFolderPath}\\Audio";
            var fileName = Path.GetFileName(gameSound.Path);
            var newFileName = fileName.Replace(".wav", ".wem");
            var wemPath = $"{audioFolderPath}\\{newFileName}";

            // Compute hash
            var hashName = WWiseHash.Compute30(newFileName);
            Console.WriteLine("################################ newFileName: " + newFileName.ToString());
            Console.WriteLine("################################ hashName: " + hashName.ToString());

            // Load
            var createdFiles = PackFileUtil.LoadFilesFromDisk(_pfs, new PackFileUtil.FileRef(wemPath, GetExpectedFolder(compilerData), $"{hashName}.wem"));
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
