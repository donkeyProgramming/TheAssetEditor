using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Misc;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileBrowser;

namespace Editors.ImportExport.Exporting
{
    public class ExportFileContextMenuHelper : IExportFileContextMenuHelper
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEnumerable<IExporterViewModel> _exporterViewModels;

        public ExportFileContextMenuHelper(IUiCommandFactory uiCommandFactory, IEnumerable<IExporterViewModel> exporterViewModels)
        {
            _uiCommandFactory = uiCommandFactory;
            _exporterViewModels = exporterViewModels;
        }

        public bool CanExportFile(PackFile packFile) 
        {
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
