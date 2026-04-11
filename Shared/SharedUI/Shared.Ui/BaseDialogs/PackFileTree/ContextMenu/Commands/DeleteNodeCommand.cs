using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DeleteNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Delete";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                standardDialogs.ShowDialogBox("Unable to edit CA packfile", "Error");
                return;
            }

            var confirmDelete = standardDialogs.ShowYesNoBox("Are you sure you want to delete the file?", "");
            if (confirmDelete == ShowMessageBoxResult.OK)
            {
                if (_selectedNode.NodeType == NodeType.File)
                    packFileService.DeleteFile(_selectedNode.FileOwner, _selectedNode.Item);
                else if (_selectedNode.NodeType == NodeType.Directory)
                    packFileService.DeleteFolder(_selectedNode.FileOwner, _selectedNode.GetFullPath());
            }
        }
    }
}
