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
        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Save";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType == NodeType.Root && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode _selectedNode, PackFile? packFile)
        {
            var packDescription = CommandLoggingHelper.DescribePack(_selectedNode.FileOwner);
            var systemPath = _selectedNode.FileOwner.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(_selectedNode.FileOwner.Name, "PackFile | *.pack", "pack");
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
                    packFileService.SavePackContainer(_selectedNode.FileOwner, systemPath, false, gameInformation);
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
