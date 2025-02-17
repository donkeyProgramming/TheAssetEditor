using System;
using Microsoft.Extensions.DependencyInjection;
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

                // Domains
                new Editors.Shared.Core.DependencyInjectionContainer(),
                new Editors.AnimationTextEditors.DependencyInjectionContainer(),
                new Editors.Reports.DependencyInjectionContainer(),
                new Editors.KitbasherEditor.DependencyInjectionContainer(),
                new Editors.AnimationMeta.DependencyInjectionContainer(),
                new Editors.Audio.DependencyInjectionContainer(),
                new Editors.TextureEditor.DependencyInjectionContainer(),
                new Editors.AnimationVisualEditors.DependencyInjectionContainer(),
                new Editors.ImportExport.DependencyInjectionContainer(),
                new Editor.VisualSkeletonEditor.DependencyInjectionContainer(),
                new Editors.AnimatioReTarget.DependencyInjectionContainer(),
                new Editors.Twui.DependencyInjectionContainer(),

                // Host application
                new DependencyInjectionContainer(),
            ];
        }

        

        public IServiceProvider Build(bool forceValidateServiceScopes, Action<IServiceCollection> replaceServices = null)
        {
            var option = new ServiceProviderOptions()
            {
                ValidateOnBuild = forceValidateServiceScopes,
                ValidateScopes = forceValidateServiceScopes,
            };

            // Add all normal classes
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, replaceServices);
            var initialServiceProvider = serviceCollection.BuildServiceProvider(option);

            //Add self and build final provider
            serviceCollection.AddSingleton<IServiceProvider>(initialServiceProvider);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Register all tools
            RegisterTools(serviceProvider.GetRequiredService<IEditorDatabase>());
            return serviceProvider;
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
