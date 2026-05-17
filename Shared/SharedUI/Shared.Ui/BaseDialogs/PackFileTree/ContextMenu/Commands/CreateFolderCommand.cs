using System.Linq;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CreateFolderCommand(IStandardDialogs standardDialogs, PackFileTreeMutationService treeMutationService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CreateFolderCommand>();

        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Create Folder";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType != NodeType.File && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode selectedNode, PackFile? packFile)
        {
            if (selectedNode.FileOwner.IsCaPackFile)
            {
                _logger.Here().Warning($"Create folder blocked for CA pack '{CommandLoggingHelper.DescribePack(selectedNode.FileOwner)}'");
                standardDialogs.ShowDialogBox("Unable to edit CA packfile");
                return;
            }

            var folderName = standardDialogs.ShowFolderNameDialog(selectedNode.Children.Select(x => x.Name), "");

            if (folderName.Any())
            {
                _logger.Here().Information($"Creating folder '{folderName}' under '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
                treeMutationService.CreateDirectoryChild(selectedNode, folderName);
            }
            else
            {
                _logger.Here().Information($"Create folder cancelled under '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
            }
        }
    }
}
