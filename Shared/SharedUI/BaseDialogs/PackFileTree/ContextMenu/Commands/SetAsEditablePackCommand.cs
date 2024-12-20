using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SetAsEditablePackCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Set as Editable Pack";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            packFileService.SetEditablePack(selectedNode.FileOwner);
        }
    }
}
