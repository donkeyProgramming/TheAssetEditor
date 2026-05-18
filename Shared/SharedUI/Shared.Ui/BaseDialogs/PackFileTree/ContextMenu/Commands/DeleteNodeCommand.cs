using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class DeleteNodeCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<DeleteNodeCommand>();

        public string GetDisplayName(TreeNode node) => "Delete";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            var packFile = TreeNodeHelper.GetPackFile(node);
            return container is { IsCaPackFile: false } && ((node.NodeType == NodeType.File && packFile != null) || node.NodeType == NodeType.Directory);
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(_selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Delete blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            if (container.IsCaPackFile)
            {
                _logger.Here().Warning($"Delete blocked for CA pack node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to edit CA packfile", "Error");
                return;
            }

            var confirmDelete = standardDialogs.ShowYesNoBox("Are you sure you want to delete the file?", "");
            if (confirmDelete == ShowMessageBoxResult.OK)
            {
                if (_selectedNode.NodeType == NodeType.File)
                {
                    var packFile = TreeNodeHelper.GetPackFile(_selectedNode);
                    if (packFile == null)
                        return;

                    _logger.Here().Information($"Deleting file node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                    packFileService.DeleteFile(container, packFile);
                }
                else if (_selectedNode.NodeType == NodeType.Directory)
                {
                    _logger.Here().Information($"Deleting directory node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                    packFileService.DeleteFolder(container, _selectedNode.GetFullPath());
                }
            }
            else
            {
                _logger.Here().Information($"Delete cancelled for node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
        }
    }
}
