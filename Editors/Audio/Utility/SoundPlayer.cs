using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.GameSettings.Warhammer3.Languages;

namespace Editors.Audio.Utility
{
    public class SoundPlayer
    {
        private static readonly ILogger s_logger = Logging.Create<SoundPlayer>();
        private readonly IPackFileService _packFileService;
        private readonly VgStreamWrapper _vgStreamWrapper;

        private static string AudioFolderName => $"{DirectoryHelper.Temp}\\Audio";

        public SoundPlayer(IPackFileService packFileService, VgStreamWrapper vgStreamWrapper)
        {
            _packFileService = packFileService;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public bool ConvertWemToWav(string wemFileName)
        {
            if (wemFileName == null || wemFileName == string.Empty)
            {
                s_logger.Here().Warning("Invalid wem file; input is empty.");
                return false;
            }

            var audioFile = FindWemFile(_packFileService, wemFileName);
            if (audioFile == null)
            {
                s_logger.Here().Error($"Unable to find wem file '{wemFileName}'.");
                return true;
            }

            return ConvertWemToWav(wemFileName, audioFile.DataSource.ReadData());
        }

        public bool ConvertWemToWav(string wemFileName, byte[] wemBytes)
        {
            var result = _vgStreamWrapper.ConvertWemToWav(wemFileName, wemBytes);
            if (result.IsSuccess)
            {
                s_logger.Here().Information($"Wem file converted to wav.");
                PlayWavFileFromDisk(result.Item);
            }
            else
                s_logger.Here().Error("Unable to export wav file.");

            return result.IsSuccess;
        }

        public bool ConvertWemToWav(IAudioRepository audioRepository, uint sourceId, uint dataSoundbankId, int fileOffset, int byteCount)
        {
            var dataSoundbankNameWithoutExtension = audioRepository.GetNameFromHash(dataSoundbankId, out bool found);
            if (!found)
            {
                s_logger.Here().Warning($"Unable to find a name from hash '{dataSoundbankId}'.");
                return false;
            }

            var dataSoundbankFileName = $"{dataSoundbankNameWithoutExtension}.bnk";
            var packFile = audioRepository.PackFileMap[dataSoundbankFileName];
            if (packFile == null)
            {
                s_logger.Here().Warning($"Unable to find packfile with name '{dataSoundbankFileName}'.");
                return false;
            }

            var byteChunk = packFile.DataSource.ReadDataAsChunk();
            byteChunk.Advance(fileOffset);
            var wemBytes = byteChunk.ReadBytes(byteCount);

            var outputFileName = $"{sourceId} - {dataSoundbankNameWithoutExtension} extract";
            return ConvertWemToWav(outputFileName, wemBytes);
        }

        public static void PlayWavFileFromDisk(string audioFile)
        {
            s_logger.Here().Information($"Playing: {audioFile}");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(audioFile)
            {
                UseShellExecute = true
            };
            process.Start();
        }

        public static PackFile FindWemFile(IPackFileService packFileService, string soundId)
        {
            var audioFile = packFileService.FindFile($"audio\\wwise\\{soundId}.wem");

            foreach (var languageEnum in Enum.GetValues<GameLanguage>().Cast<GameLanguage>())
            {
                var language = GameLanguageToStringMap[languageEnum];

                if (audioFile == null)
                    audioFile = packFileService.FindFile($"audio\\wwise\\{language}\\{soundId}.wem");
                else break;
            }

            audioFile ??= packFileService.FindFile($"audio\\{soundId}.wem");
            return audioFile;
        }

        public static void PlayWavFileFromPack(IPackFileService packFileService, AudioFilesTreeNode wavFile)
        {
            s_logger.Here().Information($"Playing: {wavFile}");

            var audioFile = FindWavFile(packFileService, wavFile.FilePath);
            if (audioFile == null)
                s_logger.Here().Error($"Unable to find wem file '{wavFile}'.");

            var wavDiskPath = $"{AudioFolderName}\\{wavFile.Name}";
            ExportFile(wavDiskPath, audioFile.DataSource.ReadData());

            PlayWavFileFromDisk(wavDiskPath);
        }

        public static PackFile FindWavFile(IPackFileService packFileService, string wavFile)
        {
            var audioFile = packFileService.FindFile(wavFile);
            return audioFile;
        }

        public static Result<bool> ExportFile(string filePath, byte[] bytes)
        {
            try
            {
                DirectoryHelper.EnsureFileFolderCreated(filePath);
                File.WriteAllBytes(filePath, bytes);
                s_logger.Here().Information("All bytes written to file");
                return Result<bool>.FromOk(true);
            }
            catch (Exception e)
            {
                s_logger.Here().Error(e.Message);
                return Result<bool>.FromError("Write error", e.Message);
            }
        }
    }
}
