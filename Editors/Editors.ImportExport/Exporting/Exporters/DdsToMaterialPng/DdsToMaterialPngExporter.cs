namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{
    public class DdsToMaterialPngExporterSettings : ExportSettingsBase
    {
        public bool ConvertNormals { get; set; }
    }

    public class DdsToMaterialPngExporter : IExporter
    {
        public string Name => "Dds_to_Png_Material";

        public void Export(DdsToMaterialPngExporterSettings settings)
        {

        }
    }
}
