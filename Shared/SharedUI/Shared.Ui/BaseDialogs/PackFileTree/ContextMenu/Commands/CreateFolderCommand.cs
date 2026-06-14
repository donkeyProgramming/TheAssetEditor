using System.Linq;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CreateFolderCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<CreateFolderCommand>();

        public string GetDisplayName(TreeNode node) => "Create Folder";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType != NodeType.File && container is { IsReadOnly: false };
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
                _logger.Here().Warning($"Create folder blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            if (container.IsReadOnly)
            {
                _logger.Here().Warning($"Create folder blocked for readonly pack '{CommandLoggingHelper.DescribePack(container)}'");
                standardDialogs.ShowDialogBox("Unable to edit readonly packfile");
                return;
            }

            var folderName = standardDialogs.ShowFolderNameDialog(_node.Children.Select(x => x.Name), "");

            if (folderName.Any())
            {
                _logger.Here().Information($"Creating folder '{folderName}' under '{CommandLoggingHelper.DescribeNode(_node)}'");
                PackFileTreeMutationService.CreateDirectoryChild(_node, folderName);
            }
            else
            {
                _logger.Here().Information($"Create folder cancelled under '{CommandLoggingHelper.DescribeNode(_node)}'");
            }
        }
    }
}
