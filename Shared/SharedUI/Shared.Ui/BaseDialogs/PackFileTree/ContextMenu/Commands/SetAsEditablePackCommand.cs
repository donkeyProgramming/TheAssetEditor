using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SetAsEditablePackCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SetAsEditablePackCommand>();

        public string GetDisplayName(TreeNode node) => "Set as Editable Pack";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root && container is { IsCaPackFile: false } && packFileService.GetEditablePack() != container;
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Set editable pack blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(selectedNode)}'");
                return;
            }

            _logger.Here().Information($"Setting pack file container '{CommandLoggingHelper.DescribePack(container)}' as editable");
            packFileService.SetEditablePack(container);
        }
    }
}
