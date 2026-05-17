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

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Delete";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => ((node.NodeType == NodeType.File && packFile != null) || node.NodeType == NodeType.Directory) && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
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
                    if (packFile == null)
                        return;

                    _logger.Here().Information($"Deleting file node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                    packFileService.DeleteFile(_selectedNode.FileOwner, packFile);
                }
                else if (_selectedNode.NodeType == NodeType.Directory)
                {
                    _logger.Here().Information($"Deleting directory node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                    packFileService.DeleteFolder(_selectedNode.FileOwner, _selectedNode.GetFullPath());
                }
            }
            else
            {
                _logger.Here().Information($"Delete cancelled for node '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
            }
        }
    }
}
