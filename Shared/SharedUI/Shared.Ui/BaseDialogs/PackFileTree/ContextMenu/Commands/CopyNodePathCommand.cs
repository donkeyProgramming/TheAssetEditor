using System.Windows;
using Shared.Core.PackFiles;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CopyNodePathCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CopyNodePathCommand>();

        public string GetDisplayName(TreeNode node) => "Copy full path";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && node.Item != null;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var path = packFileService.GetFullPath(_selectedNode.Item!);
            Clipboard.SetText(path);
            _logger.Here().Information($"Copied full path '{path}' from node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
        }
    }


}
