using System;
using AnimationEditor;
using AnimationMeta;
using AssetManagement;
using Audio;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Core;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;
using TextureEditor;
using View3D;

namespace AssetEditor.Services
{
    public class DependencyInjectionConfig
    {
        private readonly DependencyContainer[] _dependencyContainers;

        public DependencyInjectionConfig(bool loadResources = true)
        {
            _dependencyContainers = new DependencyContainer[]
            {
                // Shared
                new Shared.Core.DependencyInjectionContainer(),
                new Shared.Ui.DependencyInjectionContainer(),
                new Shared.GameFormats.DependencyInjectionContainer(),
                new Shared.EmbeddedResources.DependencyInjectionContainer(loadResources),
                new View3D_DependencyContainer(),
               
                // Domains
                new Editors.Shared.Core.DependencyInjectionContainer(),
                new Editors.AnimationContainers.DependencyInjectionContainer(),
                new Editors.Reports.DependencyInjectionContainer(),
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
            Logging.Configure(Serilog.Events.LogEventLevel.Information);

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
