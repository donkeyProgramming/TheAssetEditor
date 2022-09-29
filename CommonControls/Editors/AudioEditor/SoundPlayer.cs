using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace CommonControls.Editors.AudioEditor
{
    public class SoundPlayer
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
        public void ExtractSound(HircTreeItem rootNode, ICAkSound wwiseSound)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = ".wav";
            saveFileDialog.Filter = "Wav | *.wav";
            saveFileDialog.DefaultExt = "wav";
            if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            var outputName = saveFileDialog.FileName;
            outputName = outputName.Substring(0, outputName.Length - 4);
            var audioFile = _pfs.FindFile($"audio\\wwise\\{wwiseSound.GetSourceId()}.wem");
            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\english(uk)\\{wwiseSound.GetSourceId()}.wem");

            if (audioFile == null)
                return;
            VgStreamWrapper.ExportFile(outputName, audioFile.DataSource.ReadData(), out var _, false);
        }

        public bool PlaySound(HircTreeItem rootNode, ICAkSound wwiseSound)
        {
            var outputName = _lookUpHelper.GetName(rootNode.Item.Id) + "-" + wwiseSound.GetSourceId();
            return PlaySound(wwiseSound.GetSourceId(), outputName);
        }

        public bool PlaySound(uint id, string outputName = null)
        {
            if (outputName == null)
                outputName = Guid.NewGuid().ToString();

            var audioFile = _pfs.FindFile($"audio\\wwise\\{id}.wem");
           if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\english(uk)\\{id}.wem");
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
