using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters
{

    public interface IExporterViewModel
    {
        public string DisplayName { get; }
        string OutputExtension { get; }
        string OutputFilter => $"File ({OutputExtension})|*{OutputExtension}";

        /// <summary>
        /// Gives an exporter view model the source file before its view is
        /// displayed or Execute is called. Existing exporters do not need
        /// source-specific setup, so the default implementation is a no-op.
        /// </summary>
        void Initialize(PackFile exportSource) { }

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter);
        public ExportSupportEnum CanExportFile(PackFile file);
    }
}
