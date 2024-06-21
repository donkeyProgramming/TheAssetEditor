using Editors.ImportExport.Exporting.Presentation;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.WindowHandling;

namespace Editors.ImportExport.Exporting
{
    public class DisplayExportFileToolCommand
    {
        private readonly IWindowFactory _windowFactory;

        public DisplayExportFileToolCommand(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute(PackFile packFile)
        {
            var window = _windowFactory.Create<ExporterViewModel, ExportView>($"Export File - ", 400, 400);
            window.TypedContext.Initialize(packFile);
            window.ShowWindow(true);
        }
    }
}
