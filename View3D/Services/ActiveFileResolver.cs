using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Services
{
    public class ActiveFileResolver
    {
        private readonly PackFileService _packFileService;

        public ActiveFileResolver(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public string ActiveFileName { get; set; }
        public PackFile Get() => _packFileService.FindFile(ActiveFileName);
    }

}
