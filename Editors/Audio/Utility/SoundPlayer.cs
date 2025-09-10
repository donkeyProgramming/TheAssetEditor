using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.Utility
{
    public class SoundPlayer
    {
        private readonly ILogger _logger = Logging.Create<SoundPlayer>();
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly VgStreamWrapper _vgStreamWrapper;

        private static string AudioFolderName => $"{DirectoryHelper.Temp}\\Audio";

        public SoundPlayer(IPackFileService packFileService, IAudioRepository audioRepository, VgStreamWrapper vgStreamWrapper)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public void PlayStreamedWem(string wemFileName)
        {
            if (wemFileName == null || wemFileName == string.Empty)
                _logger.Here().Warning("Invalid wem file; input is empty.");

            var audioFile = FindWemFile(wemFileName);
            if (audioFile == null)
                _logger.Here().Error($"Unable to find wem file '{wemFileName}'.");

            var result = ConvertWemToWav(wemFileName, audioFile.DataSource.ReadData());
            if (result.IsSuccess)
            {
                _logger.Here().Information($"Playing wav file.");
                PlayWav(result.Item);
            }
            else
                _logger.Here().Error("Unable to play wav file.");
        }

        public void PlayDataWem(uint sourceId, uint dataSoundbankId, int fileOffset, int byteCount)
        {
            var dataSoundbankNameWithoutExtension = _audioRepository.GetNameFromId(dataSoundbankId, out var found);
            if (!found)
                _logger.Here().Warning($"Unable to find a name from hash '{dataSoundbankId}'.");

            var dataSoundbankFileName = $"{dataSoundbankNameWithoutExtension}.bnk";
            var packFile = _audioRepository.PackFileByBnkName[dataSoundbankFileName];
            if (packFile == null)
                _logger.Here().Warning($"Unable to find packfile with name '{dataSoundbankFileName}'.");

            var byteChunk = packFile.DataSource.ReadDataAsChunk();
            byteChunk.Advance(fileOffset);
            var wemBytes = byteChunk.ReadBytes(byteCount);

            var outputFileName = $"{sourceId} - {dataSoundbankNameWithoutExtension} extract";
            
            var result = ConvertWemToWav(outputFileName, wemBytes);
            if (result.IsSuccess)
            {
                _logger.Here().Information($"Playing wav file.");
                PlayWav(result.Item);
            }
            else
                _logger.Here().Error("Unable to play wav file.");
        }

        private Result<string> ConvertWemToWav(string sourceId, byte[] wemBytes)
        {
            _logger.Here().Information($"Trying to export '{sourceId}.wem' - {wemBytes.Length} bytes");

            var wemFileName = $"{sourceId}.wem";
            var wavFileName = $"{sourceId}.wav";
            var wemFilePath = $"{AudioFolderName}\\{wemFileName}";
            var wavFilePath = $"{AudioFolderName}\\{wavFileName}";

            ExportFileToAEFolder(wemFileName, wemBytes);

            return _vgStreamWrapper.ConvertFileUsingVgStream(wemFilePath, wavFilePath);
        }

        public void PlayWav(string wavFilePath)
        {
            _logger.Here().Information($"Playing: {wavFilePath}");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(wavFilePath)
            {
                UseShellExecute = true
            };
            process.Start();
        }

        private PackFile FindWemFile(string wemId)
        {
            var wemFile = _packFileService.FindFile($"audio\\wwise\\{wemId}.wem");

            foreach (var languageEnum in Enum.GetValues<Wh3GameLanguage>().Cast<Wh3GameLanguage>())
            {
                var language = Wh3LanguageInformation.GetGameLanguageAsString(languageEnum);
                if (wemFile == null)
                    wemFile = _packFileService.FindFile($"audio\\wwise\\{language}\\{wemId}.wem");
                else 
                    break;
            }

            wemFile ??= _packFileService.FindFile($"audio\\{wemId}.wem");
            return wemFile;
        }

        public void ExportFileToAEFolder(string fileName, byte[] bytes)
        {
            try
            {
                var wemFilePath = $"{AudioFolderName}\\{fileName}";
                DirectoryHelper.EnsureFileFolderCreated(wemFilePath);
                File.WriteAllBytes(wemFilePath, bytes);
                _logger.Here().Information($"All bytes written to file at {wemFilePath}");
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
            }
        }
    }
}
