using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.IO;
using static Shared.Core.PackFiles.IPackFileService;

namespace Shared.Ui.Events.UiCommands
{
    public class DuplicateFileCommand: IUiCommand
    {
        private readonly IPackFileService _packFileService;

        public DuplicateFileCommand(IPackFileService packFileService) 
        {
            _packFileService = packFileService;
        }

        public void Execute(PackFile item)
        {
            var fileName = item.Name;
            var extension = "";
            if(Path.HasExtension(item.Name) == true)
            {
                var index = item.Name.IndexOf('.');
                fileName = item.Name.Substring(0, index);
                extension = item.Name.Substring(index);
            }
            var newName = fileName + "_copy" + extension;
            ReadAndSave(newName, item);
        }

        private void ReadAndSave(string newName, PackFile item)
        {
            var bytes = item.DataSource.ReadData();
            var packFile = new PackFile(newName, new MemorySource(bytes));
            var parentPath = _packFileService.GetFullPath(item);
            var path = Path.GetDirectoryName(parentPath);
            var editablePack = _packFileService.GetEditablePack();

            var fileEntry = new NewPackFileEntry(path, packFile);
            _packFileService.AddFilesToPack(editablePack, [fileEntry]);
        }
    }
}
