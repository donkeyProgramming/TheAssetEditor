using AssetManagement.AssetHandling;
using AssetManagement.Strategies.Fbx.Importers;
using CommonControls;
using CommonControls.Interfaces.AssetManagement;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement
{
    public class AssetManagement_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAssetImporterProvider, AssetImporterProvider>();
            serviceCollection.AddTransient<IAssetImporter, FbxAssetImporter>();
        }
    }
}
