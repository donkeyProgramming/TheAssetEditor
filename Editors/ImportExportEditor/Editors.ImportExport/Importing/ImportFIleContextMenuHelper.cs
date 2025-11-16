using System.IO;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Misc;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;
using TreeNode = Shared.Ui.BaseDialogs.PackFileTree.TreeNode;

namespace Editors.ImportExport.Importing
{
    public class ImportFileContextMenuHelper : IImportFileContextMenuHelper
    {

        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEnumerable<IExporterViewModel> _exporterViewModels;
        private readonly ApplicationSettingsService _applicationSettings;

        public ImportFileContextMenuHelper(IUiCommandFactory uiCommandFactory, IEnumerable<IExporterViewModel> exporterViewModels, ApplicationSettingsService applicationSettings)
        {
            _uiCommandFactory = uiCommandFactory;
            _exporterViewModels = exporterViewModels;
            _applicationSettings = applicationSettings;
        }

        public bool CanImportFile(PackFile filePath)
        {
            if (FileExtensionHelper.IsGltfFile(filePath.Name)) // mess to make sure the extension is case insensitive
            {
                return true;
            }

            return false;
        }

        public void ShowDialog(TreeNode clickedNode) =>
                _uiCommandFactory.Create<DisplayImportFileToolCommand>().Execute(clickedNode.FileOwner, clickedNode.GetFullPath());
    }
}
