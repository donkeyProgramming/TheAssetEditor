using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ClosePackContainerFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Close";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Root;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            if (standardDialogs.ShowYesNoBox("Are you sure you want to close the packfile?", "") == ShowMessageBoxResult.OK)
                packFileService.UnloadPackContainer(selectedNode.FileOwner);
        }
    }


}
