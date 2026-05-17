using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExpandNodeCommand() : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ExpandNodeCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Expand all";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType != NodeType.File;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            _logger.Here().Information($"Expanding node '{CommandLoggingHelper.DescribeNode(_selectedNode)}' recursively");
            ExpandAllRecursive(_selectedNode);
        }

        void ExpandAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllRecursive(child);
        }

    }
}
