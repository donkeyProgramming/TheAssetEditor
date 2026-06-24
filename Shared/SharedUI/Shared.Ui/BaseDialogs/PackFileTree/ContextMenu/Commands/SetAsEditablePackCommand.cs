using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SetAsEditablePackCommand(IPackFileService packFileService, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<SetAsEditablePackCommand>();

        public string GetDisplayName(TreeNode node) => "Set as Editable Pack";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root && container is { IsReadOnly: false } && packFileService.GetEditablePack() != container;
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
                _logger.Here().Warning($"Set editable pack blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                return;
            }

            _logger.Here().Information($"Setting pack file container '{CommandLoggingHelper.DescribePack(container)}' as editable");
            packFileService.SetEditablePack(container);
        }
    }
}
