using System.Collections.Generic;

namespace ___AssetManagment.AssetManagers
{
    public interface IAssetImporterProvider
    {
        List<IAssetImporter> GetAllImporters();
        T GetImporter<T>() where T : IAssetImporter;
        IAssetImporter GetImporter(string format);
    }
}
