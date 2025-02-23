using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData;
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
            var dataSoundbankNameWithoutExtension = audioRepository.GetNameFromID(dataSoundbankId, out bool found);
            if (!found)
            {
                s_logger.Here().Warning($"Unable to find a name from hash '{dataSoundbankId}'.");
                return false;
            }

            var dataSoundbankFileName = $"{dataSoundbankNameWithoutExtension}.bnk";
            var packFile = audioRepository.BnkPackFileLookupByName[dataSoundbankFileName];
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

        public void PlayWavFileFromPack(AudioFilesTreeNode wavFileNode)
        {
            s_logger.Here().Information($"Playing: {wavFileNode.Name}");

            var wavFile = _packFileService.FindFile(wavFileNode.FilePath);
            if (wavFile == null)
                s_logger.Here().Error($"Unable to find wem file '{wavFileNode.FilePath}'.");

            var wavDiskPath = $"{AudioFolderName}\\{wavFileNode.Name}";
            ExportFile(wavDiskPath, wavFile.DataSource.ReadData());

            PlayWavFileFromDisk(wavDiskPath);
        }

        public static Result<bool> ExportFile(string filePath, byte[] bytes)
        {
            try
            {
                DirectoryHelper.EnsureFileFolderCreated(filePath);
                File.WriteAllBytes(filePath, bytes);
                s_logger.Here().Information($"All bytes written to file at {filePath}");
                return Result<bool>.FromOk(true);
            }
            catch (Exception e)
            {
                s_logger.Here().Error(e.Message);
                return Result<bool>.FromError("Write error", e.Message);
            }
        }

        public void ExportWavFileWithWemID(Sound audioProjectSound)
        {
            var wavFile = _packFileService.FindFile(audioProjectSound.WavFilePath);
            if (wavFile == null)
                throw new Exception($"Unable to find wav file '{audioProjectSound.WavFilePath}' in pack.");

            var wavDiskPath = $"{AudioFolderName}\\{audioProjectSound.SourceID}.wav";
            ExportFile(wavDiskPath, wavFile.DataSource.ReadData());
        }
    }
}
