using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System;
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
            try
            {
                var fileName = item.Name.Substring(0, item.Name.IndexOf("."));
                var extension = item.Name.Substring(item.Name.IndexOf("."));
                var newName = fileName + "_copy" + extension;

                readAndSave(fileName, newName, item);
            }
            catch (ArgumentOutOfRangeException)
            {
                var fileName = item.Name;
                var newName = fileName + "_copy";

                readAndSave(fileName, newName, item);
            }
        }

        public void readAndSave(string filename, string newName, PackFile item)
        {
            var bytes = item.DataSource.ReadData();
            var packFile = new PackFile(newName, new MemorySource(bytes));
            var parentPath = _packFileService.GetFullPath(item);
            var packContainer = _packFileService.GetPackFileContainer(item);
            var path = Path.GetDirectoryName(parentPath);

            _packFileService.AddFileToPack(_packFileService.GetEditablePack(), path, packFile);
        }


    }
}
