using System.Collections.Generic;

namespace CommonControls.Interfaces.AssetManagement
{
    public interface IAssetExporterProvider
    {
        List<IAssetExporter> GetAllExporters();
        T GetExporter<T>() where T : IAssetExporter;
        IAssetExporter GetExporter(string format);
    }
}
