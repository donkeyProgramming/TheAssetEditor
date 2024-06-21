using Shared.Core.Misc;

namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{
    public class DdsToMaterialPngViewModel : IExporterViewModel
    {
        private readonly DdsToMaterialPngExporter _exporter;
        public Type ViewType => typeof(DdsToMaterialPngView);
        public IExporter Exporter { get => _exporter; }
        public NotifyAttr<bool> ConvertNormals { get; set; } = new NotifyAttr<bool>(true);

        public DdsToMaterialPngViewModel()
        {
            _exporter = new DdsToMaterialPngExporter();
        }

        public void Execute(string outputPath, bool generateImporter)
        {
            _exporter.Export(new DdsToMaterialPngExporterSettings() { ConvertNormals = ConvertNormals.Value });
        }
    }
}
