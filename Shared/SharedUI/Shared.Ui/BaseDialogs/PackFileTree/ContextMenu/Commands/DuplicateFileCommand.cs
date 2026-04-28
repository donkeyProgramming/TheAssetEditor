using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DuplicateFileCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Duplicate";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode) => Execute(_selectedNode.Item);

        public void Execute(PackFile item)
        {
            var fileName = item.Name;
            var extension = "";
            if (Path.HasExtension(item.Name) == true)
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
            var parentPath = packFileService.GetFullPath(item);
            var path = Path.GetDirectoryName(parentPath);
            var editablePack = packFileService.GetEditablePack();

            var fileEntry = new NewPackFileEntry(path, packFile);
            packFileService.AddFilesToPack(editablePack, [fileEntry]);
        }
    }


}
//_uiCommandFactory.Create<DuplicateFileCommand>().Execute(_selectedNode.Item);
