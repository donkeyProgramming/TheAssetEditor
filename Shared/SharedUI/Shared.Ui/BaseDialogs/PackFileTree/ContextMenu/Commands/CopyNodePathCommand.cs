using System.Windows;
using Shared.Core.PackFiles.Models;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CopyNodePathCommand() : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CopyNodePathCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Copy full path";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType == NodeType.File;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            var path = _selectedNode.GetFullPath();
            Clipboard.SetText(path);
            _logger.Here().Information($"Copied full path '{path}' from node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
        }
    }


}
