using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    class SoundPlayer
    {
        ILogger _logger = Logging.Create<SoundPlayer>();
        private readonly PackFileService _pfs;
        private readonly WWiseNameLookUpHelper _lookUpHelper;
        private readonly string _language = "english(uk)";

        public SoundPlayer(PackFileService pfs, WWiseNameLookUpHelper lookUpHelper)
        {
            _pfs = pfs;
            _lookUpHelper = lookUpHelper;
        }

        public bool PlaySound(HircTreeItem rootNode, ICAkSound wwiseSound)
        {
            var outputName = _lookUpHelper.GetName(rootNode.Item.Id) + "-" + wwiseSound.GetSourceId();

            var audioFile = _pfs.FindFile($"audio\\wwise\\{wwiseSound.GetSourceId()}.wem");
            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\{_language}\\{wwiseSound.GetSourceId()}.wem");

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
