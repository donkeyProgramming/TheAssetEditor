using System.Linq;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class OnRenameNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Rename";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var FileOwner = _selectedNode.FileOwner;
            if (FileOwner.IsCaPackFile)
            {
                standardDialogs.ShowDialogBox("Unable to edit CA packfile", "Error");
                return;
            }

            if (_selectedNode.NodeType == NodeType.Directory)
            {
                var newFolderName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFolderName.Any())
                {
                    _selectedNode.Name = newFolderName;
                    packFileService.RenameDirectory(_selectedNode.FileOwner, _selectedNode.GetFullPath(), newFolderName);
                }

            }
            else if (_selectedNode.NodeType == NodeType.File)
            {
                var newFileName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFileName.Any())
                    packFileService.RenameFile(_selectedNode.FileOwner, _selectedNode.Item, newFileName);

            }
        }
    }
}
