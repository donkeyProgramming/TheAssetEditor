using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ImportFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IFileSystemAccess fileSystemAccess) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Import File";
        public bool ShouldAdd(TreeNode node) => node.NodeType != NodeType.File && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                standardDialogs.ShowDialogBox("Unable to edit CA packfile");
                return;
            }

            var dialogResult = standardDialogs.ShowSystemOpenFileDialog(multiselect: true);
            if (dialogResult.Result)
            {
                var parentPath = _selectedNode.GetFullPath();
                var files = dialogResult.FilePaths;
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var packFile = new PackFile(fileName, new MemorySource(fileSystemAccess.FileReadAllBytes(file)));
                    var item = new NewPackFileEntry(parentPath, packFile);
                    packFileService.AddFilesToPack(_selectedNode.FileOwner, [item]);
                }
            }
        }
    }



}
