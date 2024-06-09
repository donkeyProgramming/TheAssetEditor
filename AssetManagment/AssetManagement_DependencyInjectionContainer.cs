﻿using AssetManagement.AssetHandling;
using AssetManagement.Strategies.Fbx;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Ui.Interfaces.AssetManagement;

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
