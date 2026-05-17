using Shared.Core.Events;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public interface IContextMenuCommand : IUiCommand
    {
        public string GetDisplayName(TreeNode node, PackFile? packFile);
        public bool ShouldAdd(TreeNode node, PackFile? packFile);
        public bool IsEnabled(TreeNode node, PackFile? packFile);
        public void Execute(TreeNode node, PackFile? packFile);
    }
}
