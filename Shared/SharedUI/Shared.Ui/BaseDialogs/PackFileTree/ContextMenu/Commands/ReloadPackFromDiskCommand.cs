using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    // Picks up changes another program (e.g. RPFM) saved to a pack opened from a .pack file
    public class ReloadPackFromDiskCommand(PackReloader packReloader, LocalizationManager localizationManager) : IContextMenuCommand
    {
        private const string DisplayNameKey = "PackFileTree.ContextMenu.ReloadFromDisk";
        private TreeNode _node = null!;

        public string GetDisplayName(TreeNode node)
        {
            var localizedText = localizationManager.Get(DisplayNameKey);
            return localizedText == DisplayNameKey ? "Reload from disk" : localizedText;
        }

        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root
                && container is { IsCaPackFile: false, ContainerType: PackFileContainerType.Normal }
                && string.IsNullOrWhiteSpace(container.SystemFilePath) == false
                && File.Exists(container.SystemFilePath);
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var container = TreeNodeHelper.GetPackFileContainer(_node);
            if (container != null)
                packReloader.ConfirmAndReload(container);
        }
    }
}
