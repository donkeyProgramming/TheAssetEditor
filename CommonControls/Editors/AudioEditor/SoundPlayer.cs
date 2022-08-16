using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        public void PlaySound(HircTreeItem rootNode, ICAkSound wwiseSound)
        {
            var outputName = _lookUpHelper.GetName(rootNode.Item.Id) + "-" + wwiseSound.GetSourceId();

            var audioFile = _pfs.FindFile($"audio\\wwise\\{wwiseSound.GetSourceId()}.wem");
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

                //Process.Start(soundPath);

                //using Process fileopener = new Process();
                //
                //fileopener.StartInfo.FileName = "explorer";
                //fileopener.StartInfo.Arguments = "\"" + soundPath + "\"";
                //fileopener.Start();

            }

        }

    }
}
