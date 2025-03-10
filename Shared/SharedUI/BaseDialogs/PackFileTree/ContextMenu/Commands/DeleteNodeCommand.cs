using System.Windows;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DeleteNodeCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Delete";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete the file?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (_selectedNode.NodeType == NodeType.File)
                    packFileService.DeleteFile(_selectedNode.FileOwner, _selectedNode.Item);
                else if (_selectedNode.NodeType == NodeType.Directory)
                    packFileService.DeleteFolder(_selectedNode.FileOwner, _selectedNode.GetFullPath());
            }
        }
    }
}
