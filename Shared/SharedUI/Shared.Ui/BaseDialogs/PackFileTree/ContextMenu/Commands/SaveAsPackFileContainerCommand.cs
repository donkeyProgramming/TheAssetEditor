using System;
using System.Windows.Forms;
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
            using var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = _selectedNode.FileOwner.Name;
            saveFileDialog.Filter = "PackFile | *.pack";
            saveFileDialog.DefaultExt = "pack";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            using (new WaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    packFileService.SavePackContainer(_selectedNode.FileOwner, saveFileDialog.FileName, false, gameInformation);
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
