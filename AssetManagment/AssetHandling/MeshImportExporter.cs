using Shared.Ui.Interfaces.AssetManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetManagement.AssetHandling
{
    public class AssetImporterProvider : IAssetImporterProvider
    {
        private readonly IEnumerable<IAssetImporter> _importers;

        public AssetImporterProvider(IEnumerable<IAssetImporter> importers)
        {
            _importers = importers;
        }

        public List<IAssetImporter> GetAllImporters() => _importers.ToList();

        public T GetImporter<T>() where T : IAssetImporter
        {
            throw new NotImplementedException();
        }

        public IAssetImporter GetImporter(string format)
        {
            var importer = _importers.Where(x => IsValid(format, x)).FirstOrDefault();
            if (importer == null)
                throw new Exception($"No importer found for {format}");
            return importer;
        }

        bool IsValid(string format, IAssetImporter importer)
        {
            var res = importer.Formats.Any(x => x.Equals(format, StringComparison.InvariantCultureIgnoreCase));
            return res;
        }
    }
}
