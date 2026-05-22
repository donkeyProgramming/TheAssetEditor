using Shared.Core.Events;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public interface IContextMenuCommand : IAeCommand
    {
        public string GetDisplayName(TreeNode node);
        public bool ShouldAdd(TreeNode node);
        public bool IsEnabled(TreeNode node);
        public void Configure(TreeNode node);
    }
}
