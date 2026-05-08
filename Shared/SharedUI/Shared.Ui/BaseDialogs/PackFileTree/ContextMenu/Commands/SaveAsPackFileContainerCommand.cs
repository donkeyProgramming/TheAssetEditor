using System;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class SaveAsPackFileContainerCommand(IPackFileService packFileService, ApplicationSettingsService applicationSettingsService, IStandardDialogs standardDialogs) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SaveAsPackFileContainerCommand>();
        public string GetDisplayName(TreeNode node) => "Save As";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Root && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var saveDialogResult = standardDialogs.ShowSystemSaveFileDialog(_selectedNode.FileOwner.Name, "PackFile | *.pack", "pack");
            if (!saveDialogResult.Result || string.IsNullOrEmpty(saveDialogResult.FilePath))
                return;

            using (new WaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    packFileService.SavePackContainer(_selectedNode.FileOwner, saveDialogResult.FilePath, false, gameInformation);
                    _selectedNode.UnsavedChanged = false;
                    _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
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
