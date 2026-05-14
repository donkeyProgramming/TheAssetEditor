using Shared.Core.PackFiles;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SetAsEditablePackCommand(IPackFileService packFileService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SetAsEditablePackCommand>();

        public string GetDisplayName(TreeNode node) => "Set as Editable Pack";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Root && !node.FileOwner.IsCaPackFile && packFileService.GetEditablePack() != node.FileOwner;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            _logger.Here().Information($"Setting pack file container '{CommandLoggingHelper.DescribePack(selectedNode.FileOwner)}' as editable");
            packFileService.SetEditablePack(selectedNode.FileOwner);
        }
    }
}
