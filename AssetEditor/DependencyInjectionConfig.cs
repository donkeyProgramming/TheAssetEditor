using System;
using AnimationEditor;
using AnimationMeta;
using AssetManagement;
using Audio;
using CommonControls;
using CommonControls.Services.ToolCreation;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using TextureEditor;
using View3D;


namespace AssetEditor
{
    public class DependencyInjectionConfig
    {
        private readonly DependencyContainer[] _dependencyContainers;

        public DependencyInjectionConfig(bool loadResources = true)
        {
            _dependencyContainers = new DependencyContainer[]
            {
                // Core
                new CommonControls_DependencyInjectionContainer(loadResources),
                new View3D_DependencyContainer(),

                // Editors
                new KitbasherEditor_DependencyInjectionContainer(),
                new AssetManagement_DependencyInjectionContainer(),
                new AnimationMeta_DependencyInjectionContainer(),
                new AudioEditor_DependencyInjectionContainer(),
                new TextureEditor_DependencyInjectionContainer(),
                new AnimationEditors_DependencyInjectionContainer(),

                // Host application
                new AssetEditor_DependencyInjectionContainer(),
            };
        }

        public IServiceProvider Build()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            RegisterTools(serviceProvider.GetService<IToolFactory>());
            return serviceProvider;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            foreach (var container in _dependencyContainers)
                container.Register(services);
        }

        void RegisterTools(IToolFactory factory)
        {
            foreach (var container in _dependencyContainers)
                container.RegisterTools(factory);
        }
    }
}
