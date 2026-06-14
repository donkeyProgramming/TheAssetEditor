using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DeleteNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<DeleteNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Delete";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var packFile = TreeNodeHelper.GetPackFile(node);
            return container is { IsReadOnly: false } && ((node.NodeType == NodeType.File && packFile != null) || node.NodeType == NodeType.Directory);
        }

        public bool IsEnabled(TreeNode node) => true;

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var container = TreeNodeHelper.GetPackFileContainer(_node);
            if (container == null)
            {
                _logger.Here().Warning($"Delete blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            if (container.IsReadOnly)
            {
                _logger.Here().Warning($"Delete blocked for readonly pack node '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to edit readonly packfile", "Error");
                return;
            }

            var confirmDelete = standardDialogs.ShowYesNoBox("Are you sure you want to delete the file?", "");
            if (confirmDelete == ShowMessageBoxResult.OK)
            {
                if (_node.NodeType == NodeType.File)
                {
                    var packFile = TreeNodeHelper.GetPackFile(_node);
                    if (packFile == null)
                        return;

                    _logger.Here().Information($"Deleting file node '{CommandLoggingHelper.DescribeNode(_node)}'");
                    packFileService.DeleteFile(container, packFile);
                }
                else if (_node.NodeType == NodeType.Directory)
                {
                    _logger.Here().Information($"Deleting directory node '{CommandLoggingHelper.DescribeNode(_node)}'");
                    packFileService.DeleteFolder(container, _node.GetFullPath());
                }
            }
            else
            {
                _logger.Here().Information($"Delete cancelled for node '{CommandLoggingHelper.DescribeNode(_node)}'");
            }
        }
    }
}
