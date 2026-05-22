using Editors.ImportExport.Exporting.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting
{
    public class DisplayExportFileToolCommand : IAeCommand
    {
        private readonly IAbstractFormFactory<ExportWindow> _exportWindowFactory;
        private PackFile _packFile = null!;

        public DisplayExportFileToolCommand(IAbstractFormFactory<ExportWindow> exportWindowFactory)
        {
            _exportWindowFactory = exportWindowFactory;
        }

        public void Configure(PackFile packFile)
        {
            _packFile = packFile;
        }

        public void Execute()
        {
            var window = _exportWindowFactory.Create();
            window.Initialize(_packFile);
            window.ShowDialog();
        }
    }
}
