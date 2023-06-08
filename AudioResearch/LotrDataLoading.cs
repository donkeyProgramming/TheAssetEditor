using Audio.Storage;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioResearch
{
    internal class LotrDataLoading
    {
        public void Run()
        {

            using var application = new SimpleApplication(false);

            var pfs = application.GetService<PackFileService>();
            pfs.Load(@"C:\Users\ole_k\Downloads\attila_bnks.pack", true, true);

            var audioRepo = application.GetService<IAudioRepository>();


        }
    }
}
