using System;
using System.Diagnostics;
using System.Linq;
using Serilog;
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
            if (wemFileName == null)
            {
                s_logger.Here().Warning("Input is not a valid wem file.");
                return false;
            }

            var audioFile = FindWemFile(wemFileName, _pfs);
            if (audioFile == null)
            {
                s_logger.Here().Error("Unable to find wem file.");
                return true;
            }

            var result = _vgStreamWrapper.ConvertWemToWav(wemFileName.ToString(), audioFile.DataSource.ReadData());
            if (result.IsSuccess)
            {
                s_logger.Here().Information($"Wem file converted to wav.");
                PlayWavFile(result.Item);
            }
            else
                s_logger.Here().Error("Unable to export wav file.");

            return result.IsSuccess;
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
