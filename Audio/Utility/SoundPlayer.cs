using Audio.FileFormats.WWise.Hirc;
using CommonControls.Common;
using CommonControls.Services;
using Serilog;
using System.Diagnostics;
using Audio.Storage;
using CommonControls.FileTypes.PackFiles.Models;

namespace Audio.Utility
{
    public class SoundPlayer
    {
        ILogger _logger = Logging.Create<SoundPlayer>();
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

        public bool PlaySound(uint id, string outputName)
        {
            var audioFile = FindSoundFile(_language, id);
            if (audioFile == null)
            {
                _logger.Here().Error("Unable to find sound");
                return true;
            }

            _logger.Here().Information($"Trying to play Sound '{_pfs.GetFullPath(audioFile)}'");
            var result = _vgStreamWrapper.ConvertFromWem(outputName, audioFile.DataSource.ReadData());
            if (result.IsSuccess)
            {
                _logger.Here().Information($"Sound converted, playing: '{result.Item}'");
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(result.Item)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
            else
            {
                _logger.Here().Error("Unable to export sound");
            }

            return result.IsSuccess;
        }

        PackFile FindSoundFile(string language, uint soundId)
        {
            var audioFile = _pfs.FindFile($"audio\\wwise\\{soundId}.wem");
            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\{language}\\{soundId}.wem");
            return audioFile;
        }
    }
}
