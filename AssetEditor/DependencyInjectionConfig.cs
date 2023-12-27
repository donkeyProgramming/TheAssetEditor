using System;
using AnimationEditor;
using AnimationMeta;
using AssetManagement;
using Audio;
using CommonControls;
using CommonControls.Services.ToolCreation;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .UseDefaultServiceProvider(ConfigureServiceOptions)
                .Build();

            RegisterTools(host.Services.GetService<IToolFactory>());
            return host.Services;
        }

        void ConfigureServiceOptions(ServiceProviderOptions options)
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
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
