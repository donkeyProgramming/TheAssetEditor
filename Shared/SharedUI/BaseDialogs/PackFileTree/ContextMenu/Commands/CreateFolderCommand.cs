using System.Linq;
using System.Windows;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CreateFolderCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Create Folder";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var folderName = EditFileNameDialog.ShowDialog(_selectedNode, "");

            if (folderName.Any())
                _selectedNode.Children.Add(new TreeNode(folderName, NodeType.Directory, _selectedNode.FileOwner, _selectedNode));
        }
    }
}
