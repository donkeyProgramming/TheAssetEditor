using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;
using Shared.Core.ToolCreation;

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
                new GameWorld.WpfWindow.DependencyInjectionContainer(),
                new GameWorld.Core.DependencyInjectionContainer(),
               
                // Misc
                new Editors.Shared.DevConfig.DependencyInjectionContainer(),

                // Domains
                new Editors.Shared.Core.DependencyInjectionContainer(),
                new Editors.AnimationTextEditors.DependencyInjectionContainer(),
                new Editors.Reports.DependencyInjectionContainer(),
                new KitbasherEditor.DependencyInjectionContainer(),
                new Editors.AssetManagement.DependencyInjectionContainer(),
                new Editors.AnimationMeta.DependencyInjectionContainer(),
                new Editors.Audio.DependencyInjectionContainer(),
                new Editors.TextureEditor.DependencyInjectionContainer(),
                new Editors.AnimationVisualEditors.DependencyInjectionContainer(),

                // Host application
                new DependencyInjectionContainer(),
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
            if (Debugger.IsAttached)
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            }
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
