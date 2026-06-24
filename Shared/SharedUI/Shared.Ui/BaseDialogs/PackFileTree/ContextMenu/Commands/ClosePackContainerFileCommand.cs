using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ClosePackContainerFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs, IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<ClosePackContainerFileCommand>();

        public string GetDisplayName(TreeNode node) => "Close";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Root;
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
                _logger.Here().Warning($"Close blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(container);
            if (standardDialogs.ShowYesNoBox("Are you sure you want to close the packfile?", "") == ShowMessageBoxResult.OK)
            {
                _logger.Here().Information($"Closing pack file container '{packDescription}' from context menu");
                packFileService.UnloadPackContainer(container);
            }
            else
            {
                _logger.Here().Information($"Close cancelled for pack file container '{packDescription}'");
            }
        }
    }


}
