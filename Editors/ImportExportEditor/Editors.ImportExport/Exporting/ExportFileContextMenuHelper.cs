using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Misc;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;

namespace Editors.ImportExport.Exporting
{
    public class ExportFileContextMenuHelper : IExportFileContextMenuHelper
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEnumerable<IExporterViewModel> _exporterViewModels;
        private readonly ApplicationSettingsService _applicationSettings;

        public ExportFileContextMenuHelper(IUiCommandFactory uiCommandFactory, IEnumerable<IExporterViewModel> exporterViewModels, ApplicationSettingsService applicationSettings)
        {
            _uiCommandFactory = uiCommandFactory;
            _exporterViewModels = exporterViewModels;
            _applicationSettings = applicationSettings;
        }

        public bool CanExportFile(PackFile packFile) 
        {
            if(_applicationSettings.CurrentSettings.IsDeveloperRun)
                return true;

            foreach (var exporter in _exporterViewModels)
            {
                if (exporter.CanExportFile(packFile) != ExportSupportEnum.NotSupported)
                    return true;
            }
            return false;
        }

        public void ShowDialog(PackFile packFile) => _uiCommandFactory.Create<DisplayExportFileToolCommand>().Execute(packFile);
    }
}
