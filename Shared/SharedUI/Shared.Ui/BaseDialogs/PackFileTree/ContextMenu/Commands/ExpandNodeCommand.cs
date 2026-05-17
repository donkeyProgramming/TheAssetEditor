using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExpandNodeCommand() : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ExpandNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Expand all";
        public bool ShouldAdd(TreeNode node) => node.NodeType != NodeType.File;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
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
