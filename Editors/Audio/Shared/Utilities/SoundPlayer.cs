using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.Shared.Utilities
{
    public class SoundPlayer(IPackFileService packFileService, IAudioRepository audioRepository, VgStreamWrapper vgStreamWrapper)
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly VgStreamWrapper _vgStreamWrapper = vgStreamWrapper;

        private readonly ILogger _logger = Logging.Create<SoundPlayer>();

        private static string AudioFolderName => $"{DirectoryHelper.Temp}\\Audio";

        public void PlayStreamedWem(string wemFileName)
        {
            if (wemFileName == null || wemFileName == string.Empty)
                _logger.Here().Warning("Invalid wem file; input is empty.");

            var wemFile = FindWemFile(wemFileName);
            if (wemFile == null)
                _logger.Here().Error($"Unable to find wem file '{wemFileName}'.");

            var result = ConvertWemToWav(wemFileName, wemFile.DataSource.ReadData());
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

            foreach (var languageEnum in Enum.GetValues<Wh3Language>().Cast<Wh3Language>())
            {
                var language = Wh3LanguageInformation.GetLanguageAsString(languageEnum);
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
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
            }
        }
    }
}
