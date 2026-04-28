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
    public class SavePackFileContainerCommand(
        IPackFileService packFileService,
        IStandardDialogs standardDialogs,
        ApplicationSettingsService applicationSettingsService) : IContextMenuCommand
    {
        private readonly ILogger _logger = Logging.Create<SavePackFileContainerCommand>();
        public string GetDisplayName(TreeNode node) => "Save";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode)
        {
            var systemPath = _selectedNode.FileOwner.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = _selectedNode.FileOwner.Name;
                saveFileDialog.Filter = "PackFile | *.pack";
                saveFileDialog.DefaultExt = "pack";
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;
                systemPath = saveFileDialog.FileName;
            }

            using (new WaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    packFileService.SavePackContainer(_selectedNode.FileOwner, systemPath, false, gameInformation);
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    System.Windows.MessageBox.Show("Error saving:\n\n" + e.Message, "Error");
                }
            }
        }

        public void Execute()
        {
            var pack = packFileService.GetEditablePack();
            if (pack == null)
            {
                standardDialogs.ShowDialogBox("No editable pack selected, cant save", "Error");
                return;
            }

            var systemPath = pack.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = pack.Name;
                saveFileDialog.Filter = "PackFile | *.pack";
                saveFileDialog.DefaultExt = "pack";
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;
                systemPath = saveFileDialog.FileName;
            }

            using (new WaitCursor())
            {
                try
                {
                    var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
                    packFileService.SavePackContainer(pack, systemPath, false, gameInformation);
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    System.Windows.MessageBox.Show("Error saving:\n\n" + e.Message, "Error");
                }
            }
        }
    }
}
