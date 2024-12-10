using System.Linq;
using System.Windows;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands
{
    public class OnRenameNodeCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Rename";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var FileOwner = packFileService.GetPackFileContainer(_selectedNode.Item);
            if (FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (_selectedNode.GetNodeType() == NodeType.Directory)
            {
                var newFolderName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFolderName.Any())
                {
                    _selectedNode.Name = newFolderName;
                    packFileService.RenameDirectory(_selectedNode.FileOwner, _selectedNode.GetFullPath(), newFolderName);
                }

            }
            else if (_selectedNode.GetNodeType() == NodeType.File)
            {
                var newFileName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFileName.Any())
                    packFileService.RenameFile(_selectedNode.FileOwner, _selectedNode.Item, newFileName);

            }
        }
    }





}
