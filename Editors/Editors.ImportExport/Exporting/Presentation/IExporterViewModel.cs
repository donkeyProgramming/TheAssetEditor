using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters
{
    public enum ExportSupportEnum
    { 
        Supported,
        NotSupported,
        HighPriority
    }

    public interface IExporterViewModel
    {
        public string DisplayName { get; }
        string OutputExtension { get; }

        public void Execute(string outputPath, bool generateImporter);
        public ExportSupportEnum CanExportFile(PackFile file);
    }
}
