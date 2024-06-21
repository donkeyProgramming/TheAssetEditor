namespace Editors.ImportExport.Exporting.Exporters
{
    public interface IExporterViewModel
    {
        public Type ViewType { get; }
        public IExporter Exporter { get; }

        public void Execute(string outputPath, bool generateImporter);

    }
}
