using AssetManagement.GeometryManagement;
using AssetManagement.Strategies.Fbx.AssetHandling;
using AssetManagment.Strategies.Fbx.FbxAssetHandling;
using CommonControls.Interfaces.AssetManagement;
using Microsoft.Extensions.DependencyInjection;
using CommonControls;

namespace AssetManagement
{
    public class AssetManagement_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAssetImporterProvider, AssetImporterProvider>();
            serviceCollection.AddTransient<IAssetImporter, FbxAssetImporter>();
                                    
            serviceCollection.AddScoped<IAssetExporterProvider, AssetExporterProvider>();            
            serviceCollection.AddTransient<IAssetExporter, FbxAssetExporter>();
        }
    }
}
