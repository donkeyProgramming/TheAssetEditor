using System.Windows;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CopyNodePathCommand(IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<CopyNodePathCommand>();

        public string GetDisplayName(TreeNode node) => "Copy full path";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File;
        public bool IsEnabled(TreeNode node) => true;

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var path = _node.GetFullPath();
            Clipboard.SetText(path);
            _logger.Here().Information($"Copied full path '{path}' from node '{CommandLoggingHelper.DescribeNode(_node)}'");
        }
    }


}
