using System;
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
            _dependencyContainers =
            [
                // Shared
                new Shared.Core.DependencyInjectionContainer(),
                new Shared.Ui.DependencyInjectionContainer(),
                new Shared.GameFormats.DependencyInjectionContainer(),
                new Shared.EmbeddedResources.DependencyInjectionContainer(loadResources),
                new GameWorld.Core.DependencyInjectionContainer(),
               
                // Misc

                // Domains
                new Editors.Shared.Core.DependencyInjectionContainer(),
                new Editors.AnimationTextEditors.DependencyInjectionContainer(),
                new Editors.Reports.DependencyInjectionContainer(),
                new KitbasherEditor.DependencyInjectionContainer(),
                new Editors.AnimationMeta.DependencyInjectionContainer(),
                new Editors.Audio.DependencyInjectionContainer(),
                new Editors.TextureEditor.DependencyInjectionContainer(),
                new Editors.AnimationVisualEditors.DependencyInjectionContainer(),
                new Editors.ImportExport.DependencyInjectionContainer(),
                new Editor.VisualSkeletonEditor.DependencyInjectionContainer(),

                // Host application
                new DependencyInjectionContainer(),
            ];
        }

        public IServiceProvider Build(bool forceValidateServiceScopes, Action<IServiceCollection> replaceServices = null)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(x=> ConfigureServices(x, replaceServices))
                .UseDefaultServiceProvider(x=>ConfigureServiceOptions(forceValidateServiceScopes, x))
                .Build();

            RegisterTools(host.Services.GetService<IEditorDatabase>());
            return host.Services;
        }

        void ConfigureServiceOptions(bool forceValidateServiceScopes, ServiceProviderOptions options)
        {
            if(forceValidateServiceScopes)
            { 
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            }
        }

        private void ConfigureServices(IServiceCollection services, Action<IServiceCollection> replaceServices)
        {
            Logging.Configure(Serilog.Events.LogEventLevel.Information);

            foreach (var container in _dependencyContainers)
                container.Register(services);

            replaceServices?.Invoke(services);
        }

        void RegisterTools(IEditorDatabase factory)
        {
            foreach (var container in _dependencyContainers)
                container.RegisterTools(factory);
        }
    }
}
