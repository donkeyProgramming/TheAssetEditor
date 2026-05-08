using System.Linq;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CreateFolderCommand(IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Create Folder";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            if (selectedNode.FileOwner.IsCaPackFile)
            {
                standardDialogs.ShowDialogBox("Unable to edit CA packfile");
                return;
            }

            var folderName = standardDialogs.ShowFolderNameDialog(selectedNode.Children.Select(x => x.Name), "");

            if (folderName.Any())
                selectedNode.AddDirectoryChild(folderName);
        }
    }
}
