﻿using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.IO;

namespace Shared.Ui.Events.UiCommands
{
    public class DuplicateFileCommand: IUiCommand
    {
        private readonly PackFileService _packFileService;

        public DuplicateFileCommand(PackFileService packFileService) 
        {
            _packFileService = packFileService;
        }

        public void Execute(PackFile item)
        {
            var fileName = item.Name.Substring(0, item.Name.IndexOf("."));
            var extension = item.Name.Substring(item.Name.IndexOf("."));

            var newName = fileName + "_copy" + extension;
            var bytes = item.DataSource.ReadData();
            var packFile = new PackFile(newName, new MemorySource(bytes));
            var parentPath = _packFileService.GetFullPath(item);
            var packContainer = _packFileService.GetPackFileContainer(item);
            var path = Path.GetDirectoryName(parentPath);

            _packFileService.AddFileToPack(packContainer, path, packFile);
        }
    }
}