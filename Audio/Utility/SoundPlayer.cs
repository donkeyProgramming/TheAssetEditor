using Audio.FileFormats.WWise.Hirc;
using CommonControls.Common;
using Audio.AudioEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Audio.Storage;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Audio.Utility
{
    public class SoundPlayer
    {
        ILogger _logger = Logging.Create<SoundPlayer>();

        private readonly PackFileService _pfs;
        private readonly IAudioRepository _audioRepository;
        private readonly string _language = "english(uk)";

        public SoundPlayer(PackFileService pfs, IAudioRepository audioRepository)
        {
            _pfs = pfs;
            _audioRepository = audioRepository;
        }

        public bool PlaySound(ICAkSound wwiseSound, uint parentEventId)
        {
            if (wwiseSound == null)
            {
                _logger.Here().Warning("Input is not a valid wwise sound");
                return false;
            }

            var outputName = _audioRepository.GetNameFromHash(parentEventId) + "-" + wwiseSound.GetSourceId();
            return PlaySound(wwiseSound.GetSourceId(), outputName);
        }

        public bool PlaySound(uint id, string outputName = null)
        {
            if (outputName == null)
                outputName = Guid.NewGuid().ToString();

            var audioFile = _pfs.FindFile($"audio\\wwise\\{id}.wem");
            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\{_language}\\{id}.wem");

            if (audioFile == null)
            {
                _logger.Here().Error("Unable to find sound");
                return true;
            }

            var result = VgStreamWrapper.ExportFile(outputName, audioFile.DataSource.ReadData(), out var soundPath);
            if (result)
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(soundPath)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
            else
            {
                _logger.Here().Error("Unable to export sound");
            }

            return result;
        }

    }
}
