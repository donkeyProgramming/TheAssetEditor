using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace CommonControls.Editors.AudioEditor
{
    class SoundPlayer
    {
        private readonly PackFileService _pfs;
        private readonly WWiseNameLookUpHelper _lookUpHelper;

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
        public void PlaySound(HircTreeItem rootNode, ICAkSound wwiseSound)
        {
            var outputName = _lookUpHelper.GetName(rootNode.Item.Id) + "-" + wwiseSound.GetSourceId();
            outputName = $"{VgStreamWrapper.GetAudioFolder()}\\{outputName}";

            var audioFile = _pfs.FindFile($"audio\\wwise\\{wwiseSound.GetSourceId()}.wem");
            if (audioFile == null)
                audioFile = _pfs.FindFile($"audio\\wwise\\english(uk)\\{wwiseSound.GetSourceId()}.wem");

            if (audioFile == null) 
                return;

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

        }

    }
}
