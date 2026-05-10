using Shared.Core.PackFiles;
using Shared.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ClosePackContainerFileCommand(IPackFileService packFileService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<ClosePackContainerFileCommand>();

        public string GetDisplayName(TreeNode node) => "Close";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Root;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            var packDescription = CommandLoggingHelper.DescribePack(selectedNode.FileOwner);
            if (standardDialogs.ShowYesNoBox("Are you sure you want to close the packfile?", "") == ShowMessageBoxResult.OK)
            {
                _logger.Here().Information($"Closing pack file container '{packDescription}' from context menu");
                packFileService.UnloadPackContainer(selectedNode.FileOwner);
            }
            else
            {
                _logger.Here().Information($"Close cancelled for pack file container '{packDescription}'");
            }
        }
    }


}
