using System.Collections.Generic;
using System;
using System.Diagnostics;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using SharpDX;

namespace Editors.Audio.Utility
{
    public class SoundPlayer
    {
        private static ILogger _logger = Logging.Create<SoundPlayer>();
        private readonly string _language = "english(uk)";

        private readonly PackFileService _pfs;
        private readonly IAudioRepository _audioRepository;
        private readonly VgStreamWrapper _vgStreamWrapper;

        public SoundPlayer(PackFileService pfs, IAudioRepository audioRepository, VgStreamWrapper vgStreamWrapper)
        {
            _pfs = pfs;
            _audioRepository = audioRepository;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public bool PlaySound(string sourceId, uint parentEventId)
        {
            if (sourceId == null)
            {
                _logger.Here().Warning("Input is not a valid wwise sound");
                return false;
            }

            _logger.Here().Information($"User selected {sourceId}.wem to be played");
            var outputName = $"{_audioRepository.GetNameFromHash(parentEventId)}-{sourceId}";

            var audioFile = FindSoundFile(_language, sourceId);

            if (audioFile == null)
            {
                _logger.Here().Error("Unable to find sound");
                return true;
            }

            _logger.Here().Information($"Trying to play Sound '{_pfs.GetFullPath(audioFile)}'");
            var result = _vgStreamWrapper.ConvertFromWem(outputName, audioFile.DataSource.ReadData());

            if (result.IsSuccess)
            {
                _logger.Here().Information($"Sound converted, playing: {result.Item}");
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(result.Item)
                {
                    UseShellExecute = true
                };

                p.Start();
            }

            else
                _logger.Here().Error("Unable to export sound");

            return result.IsSuccess;
        }

        public static void PlaySound(Dictionary<string, object> dataGridRowContext)
        {
            if (dataGridRowContext.TryGetValue("AudioFiles", out var value) && value is List<string> audioFiles && audioFiles.Count > 0)
            {
                var random = new Random();
                var randomIndex = random.Next(audioFiles.Count);
                var randomAudioFile = audioFiles[randomIndex];

                _logger.Here().Information($"Playing: {randomAudioFile}");

                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(randomAudioFile)
                {
                    UseShellExecute = true
                };

                p.Start();
            }
        }

        PackFile FindSoundFile(string language, string soundId)
        {
            var audioFile = _pfs.FindFile($"audio\\wwise\\{soundId}.wem");

            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\{language}\\{soundId}.wem");

            if (audioFile == null) // Attila
                audioFile = _pfs.FindFile($"audio\\{soundId}.wem");

            return audioFile;
        }
    }
}
