using System.Linq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CreateFolderCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, PackFileTreeMutationService treeMutationService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<CreateFolderCommand>();

        public string GetDisplayName(TreeNode node) => "Create Folder";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType != NodeType.File && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Create folder blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            if (container.IsCaPackFile)
            {
                _logger.Here().Warning($"Create folder blocked for CA pack '{CommandLoggingHelper.DescribePack(container)}'");
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
