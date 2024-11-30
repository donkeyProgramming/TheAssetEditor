using Shared.Core.Events;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands
{
    public interface IContextMenuCommand : IUiCommand
    {
        public string GetDisplayName(TreeNode node);
        public bool IsEnabled(TreeNode node);
        public void Execute(TreeNode node);
    }
}
