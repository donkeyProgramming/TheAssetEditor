using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CollapseNodeCommand() : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CollapseNodeCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Collapse all";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType != NodeType.File;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            _logger.Here().Information($"Collapsing node '{CommandLoggingHelper.DescribeNode(_selectedNode)}' recursively");
            CollapsAllRecursive(_selectedNode);
        }

        void CollapsAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = false;
            foreach (var child in node.Children)
                CollapsAllRecursive(child);
        }

    }
}
