using Editors.ImportExport.Exporting.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

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
}
