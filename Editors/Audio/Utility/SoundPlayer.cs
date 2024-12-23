using System;
using System.Diagnostics;
using System.Linq;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.GameSettings.Warhammer3.Languages;

namespace Editors.Audio.Utility
{
    public class SoundPlayer
    {
        private static readonly ILogger s_logger = Logging.Create<SoundPlayer>();
        private readonly IPackFileService _pfs;
        private readonly VgStreamWrapper _vgStreamWrapper;

        public SoundPlayer(IPackFileService pfs, VgStreamWrapper vgStreamWrapper)
        {
            _pfs = pfs;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public bool ConvertWemToWav(string wemFileName)
        {
            if (wemFileName == null || wemFileName == string.Empty)
            {
                s_logger.Here().Warning("Invalid wem file; input is empty.");
                return false;
            }

            var audioFile = FindWemFile(wemFileName, _pfs);
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
                PlayWavFile(result.Item);
            }
            else
                s_logger.Here().Error("Unable to export wav file.");

            return result.IsSuccess;
        }

        public bool ConvertWemToWav(IAudioRepository audioRepository, uint sourceId, uint dataSoundbankId, int fileOffset, int byteCount)
        {
            string dataSoundbankNameWithoutExtension = audioRepository.GetNameFromHash(dataSoundbankId, out bool found);
            if (!found)
            {
                s_logger.Here().Warning($"Unable to find a name from hash '{dataSoundbankId}'.");
                return false;
            }

            string dataSoundbankFileName = $"{dataSoundbankNameWithoutExtension}.bnk";
            PackFile packFile = audioRepository.PackFileMap[dataSoundbankFileName];
            if (packFile == null)
            {
                s_logger.Here().Warning($"Unable to find packfile with name '{dataSoundbankFileName}'.");
                return false;
            }

            ByteChunk byteChunk = packFile.DataSource.ReadDataAsChunk();
            byteChunk.Advance(fileOffset);
            byte[] wemBytes = byteChunk.ReadBytes(byteCount);

            string outputFileName = $"{sourceId} - {dataSoundbankNameWithoutExtension} extract";
            return ConvertWemToWav(outputFileName, wemBytes);
        }

        public static void PlayWavFile(string audioFile)
        {
            s_logger.Here().Information($"Playing: {audioFile}");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(audioFile)
            {
                UseShellExecute = true
            };
            process.Start();
        }

        public static PackFile FindWemFile(string soundId, IPackFileService pfs)
        {
            var audioFile = pfs.FindFile($"audio\\wwise\\{soundId}.wem");

            foreach (var languageEnum in Enum.GetValues<GameLanguage>().Cast<GameLanguage>())
            {
                var language = GameLanguageToStringMap[languageEnum];

                if (audioFile == null)
                    audioFile = pfs.FindFile($"audio\\wwise\\{language}\\{soundId}.wem");
                else break;
            }

            audioFile ??= pfs.FindFile($"audio\\{soundId}.wem");
            return audioFile;
        }
    }
}
