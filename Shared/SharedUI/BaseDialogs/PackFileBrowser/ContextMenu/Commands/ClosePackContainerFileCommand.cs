using System.Windows;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands
{
    public class ClosePackContainerFileCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Close";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (MessageBox.Show("Are you sure you want to close the packfile?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                packFileService.UnloadPackContainer(_selectedNode.FileOwner);
        }
    }


}
