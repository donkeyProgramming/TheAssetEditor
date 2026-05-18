using System;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SavePackFileContainerCommand(
        IPackFileService packFileService,
        IStandardDialogs standardDialogs,
        ApplicationSettingsService applicationSettingsService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SavePackFileContainerCommand>();
        public string GetDisplayName(TreeNode node) => "Save";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(_selectedNode);
            if (container == null)
            {
                _logger.Here().Warning($"Save blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_selectedNode)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(container);
            var systemPath = container.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(container.Name, "PackFile | *.pack", "pack");
                if (!saveDialogResult.Result || string.IsNullOrEmpty(saveDialogResult.FilePath))
                {
                    _logger.Here().Information($"Save cancelled for pack file container '{packDescription}'");
                    return;
                }
                systemPath = saveDialogResult.FilePath;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    _logger.Here().Information($"Saving pack file container '{packDescription}' to '{systemPath}'");
                    packFileService.SavePackContainer(container, systemPath, false, gameInformation);
                    _logger.Here().Information($"Saved pack file container '{packDescription}' to '{systemPath}'");
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    standardDialogs.ShowDialogBox("Error saving:\n\n" + e.Message, "Error");
                }
            }
        }

        public void Execute()
        {
            var pack = packFileService.GetEditablePack();
            if (pack == null)
            {
                _logger.Here().Warning("Save requested from command without an editable pack selected");
                standardDialogs.ShowDialogBox("No editable pack selected, cant save", "Error");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(pack);
            var systemPath = pack.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(pack.Name, "PackFile | *.pack", "pack");
                if (!saveDialogResult.Result || string.IsNullOrEmpty(saveDialogResult.FilePath))
                {
                    _logger.Here().Information($"Save cancelled for editable pack '{packDescription}'");
                    return;
                }
                systemPath = saveDialogResult.FilePath;
            }

            using (standardDialogs.ShowWaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    _logger.Here().Information($"Saving editable pack '{packDescription}' to '{systemPath}'");
                    packFileService.SavePackContainer(pack, systemPath, false, gameInformation);
                    _logger.Here().Information($"Saved editable pack '{packDescription}' to '{systemPath}'");
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
