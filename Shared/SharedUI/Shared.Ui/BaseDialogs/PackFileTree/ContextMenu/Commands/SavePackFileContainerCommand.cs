using System.IO;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.ErrorHandling;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SavePackFileContainerCommand(
        IPackFileService packFileService,
        IStandardDialogs standardDialogs,
        ApplicationSettingsService applicationSettingsService,
        LocalizationManager localizationManager,
        IScopedLogger scopedLogger) : IContextMenuCommand
    {
        private readonly ILogger _logger = scopedLogger.ForContext<SavePackFileContainerCommand>();
        public string GetDisplayName(TreeNode node) => "Save";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Root && container is { IsReadOnly: false };
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
                _logger.Here().Warning($"Save blocked because no container was resolved for '{CommandLoggingHelper.DescribeNode(_node)}'");
                standardDialogs.ShowDialogBox("Unable to resolve selected packfile", "Error");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(container);
            var systemPath = ResolveSavePath(container, packDescription);
            if (string.IsNullOrWhiteSpace(systemPath))
                return;

            if (!ValidateSaveLocation(systemPath))
                return;

            Save(container, systemPath, packDescription);
        }

        public void ExecuteForEditablePack()
        {
            var pack = packFileService.GetEditablePack();
            if (pack == null)
            {
                _logger.Here().Warning("Save requested from command without an editable pack selected");
                standardDialogs.ShowDialogBox("No editable pack selected, cant save", "Error");
                return;
            }

            var packDescription = CommandLoggingHelper.DescribePack(pack);
            var systemPath = ResolveSavePath(pack, packDescription);
            if (string.IsNullOrWhiteSpace(systemPath))
                return;

            if (!ValidateSaveLocation(systemPath))
                return;

            Save(pack, systemPath, packDescription);
        }

        private void Save(IPackFileContainer container, string systemPath, string packDescription)
        {
            var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
            var backupFailure = TrySave(container, systemPath, packDescription, ShouldKeepBackup(container), gameInformation);
            if (backupFailure == null)
                return;

            // Nothing has been written yet: the user decides whether to save without the .pack1
            var message = string.Format(localizationManager.Get("SavePackFileContainerCommand.BackupFailed"),
                Path.GetFileName(backupFailure.PackFilePath), Path.GetFileName(backupFailure.BackupPath), backupFailure.InnerException?.Message);
            if (standardDialogs.ShowYesNoBox(message, localizationManager.Get("SavePackFileContainerCommand.BackupFailedTitle")) != ShowMessageBoxResult.OK)
            {
                _logger.Here().Information($"Save of '{packDescription}' cancelled after its backup failed");
                return;
            }

            TrySave(container, systemPath, packDescription, false, gameInformation);
        }

        // A .pack edited directly keeps its previous version as .pack1 on its first save. A project's
        // output pack is rebuilt from its folder, so it needs none.
        private bool ShouldKeepBackup(IPackFileContainer container)
        {
            var settings = applicationSettingsService.CurrentSettings;
            return container.ContainerType == PackFileContainerType.Normal
                && settings.EnableDirectPackEditing
                && settings.BackupPacksOnFirstSave;
        }

        // Shows any error except a failed backup, which is returned so the user can be asked about it
        private PackBackupFailedException? TrySave(IPackFileContainer container, string systemPath, string packDescription, bool createBackup, GameInformation gameInformation)
        {
            using (standardDialogs.ShowWaitCursor())
            {
                try
                {
                    _logger.Here().Information($"Saving pack file container '{packDescription}' to '{systemPath}'");
                    packFileService.SavePackContainer(container, systemPath, createBackup, gameInformation);
                    _logger.Here().Information($"Saved pack file container '{packDescription}' to '{systemPath}'");
                    return null;
                }
                catch (PackBackupFailedException e)
                {
                    _logger.Here().Warning(e, $"Couldn't back up '{packDescription}' before saving it");
                    return e;
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    standardDialogs.ShowDialogBox("Error saving:\n\n" + e.Message, "Error");
                    return null;
                }
            }
        }

        private string? ResolveSavePath(IPackFileContainer container, string packDescription)
        {
            var systemPath = container.ContainerType == PackFileContainerType.SystemFolder
                ? container.PackFileSettings.SaveLocationPath
                : container.SystemFilePath;

            if (!string.IsNullOrWhiteSpace(systemPath))
                return Path.ChangeExtension(systemPath, ".pack");

            var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(container.Name, "PackFile | *.pack", "pack");
            if (!saveDialogResult.Result || string.IsNullOrWhiteSpace(saveDialogResult.FilePath))
            {
                _logger.Here().Information($"Save cancelled for pack file container '{packDescription}'");
                return null;
            }

            systemPath = Path.ChangeExtension(saveDialogResult.FilePath, ".pack");
            if (container.ContainerType == PackFileContainerType.SystemFolder)
            {
                container.PackFileSettings.SaveLocationPath = systemPath;
                container.SaveSettings();
            }

            return systemPath;
        }

        private bool ValidateSaveLocation(string systemPath)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(systemPath);
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
            {
                standardDialogs.ShowDialogBox($"Invalid packfile output location:\n\n{systemPath}", "Error");
                return false;
            }

            var directoryPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                standardDialogs.ShowDialogBox($"Packfile output folder does not exist:\n\n{directoryPath}", "Error");
                return false;
            }

            return true;
        }
    }
}
