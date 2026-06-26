using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExpandNodeCommand(IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<ExpandNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Expand all";
        public bool ShouldAdd(TreeNode node) => node.NodeType != NodeType.File;
        public bool IsEnabled(TreeNode node) => true;

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            _logger.Here().Information($"Expanding node '{CommandLoggingHelper.DescribeNode(_node)}' recursively");
            ExpandAllRecursive(_node);
        }

        void ExpandAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllRecursive(child);
        }

    }
}
