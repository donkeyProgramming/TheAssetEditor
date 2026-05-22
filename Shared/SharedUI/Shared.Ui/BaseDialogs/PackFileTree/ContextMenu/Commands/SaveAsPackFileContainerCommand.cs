using System;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SaveAsPackFileContainerCommand(IPackFileService packFileService, ApplicationSettingsService applicationSettingsService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SaveAsPackFileContainerCommand>();
        public string GetDisplayName(TreeNode node) => "Save As";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root && container is { IsCaPackFile: false };
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
                _logger.Here().Warning($"Save As blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(container);
            var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(container.Name, "PackFile | *.pack", "pack");
            if (!saveDialogResult.Result || string.IsNullOrEmpty(saveDialogResult.FilePath))
            {
                _logger.Here().Information($"Save As cancelled for pack file container '{packDescription}'");
                return;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    _logger.Here().Information($"Saving pack file container '{packDescription}' as '{saveDialogResult.FilePath}'");
                    packFileService.SavePackContainer(container, saveDialogResult.FilePath, false, gameInformation);
                    (_node as RootTreeNode)?.UnsavedChanges.ClearAll();
                    _logger.Here().Information($"Saved pack file container '{packDescription}' as '{saveDialogResult.FilePath}'");
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    standardDialogs.ShowDialogBox("Error saving:\n\n" + e.Message, "Error");
                }
            }
        }
    }
}
