using Editors.ImportExport.Exporting.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileBrowser;

namespace Editors.ImportExport.Exporting
{
    public class DisplayExportFileToolCommand : IUiCommand
    {
        private readonly IAbstractFormFactory<ExportWindow> _exportWindowFactory;

        public DisplayExportFileToolCommand(IAbstractFormFactory<ExportWindow> exportWindowFactory)
        {
            _exportWindowFactory = exportWindowFactory;
        }

        public void Execute(PackFile packFile)
        {
            var window = _exportWindowFactory.Create();
            window.Initialize(packFile);
            window.ShowDialog();
        }
    }

    public class ExportFileContextMenuHelper : IExportFileContextMenuHelper
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public ExportFileContextMenuHelper(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public bool CanExportFile(PackFile packFile) 
        {
            return true;
        }

        public void ShowDialog(PackFile packFile) => _uiCommandFactory.Create<DisplayExportFileToolCommand>().Execute(packFile);
    }
}
