using AnimationEditor;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Audio;
using CommonControls.Common;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.AnimationPack;
using CommonControls.Editors.AnimMeta;
using CommonControls.Editors.CampaignAnimBin;
using CommonControls.Editors.TextEditor;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.Editors.Wtui;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Resources;
using CommonControls.Services;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using System;
using TextureEditor;
using View3D;
using CommonControls.Events;
using Common;

namespace AssetEditor
{
    public class DependencyInjectionConfig
    {
        DependencyContainer[] dependencyContainers = new DependencyContainer[]
        {
            new View3D_DependencyContainer(),
            new KitbasherEditor_DependencyInjectionContainer(),
        };

        public IServiceProvider ServiceProvider { get; private set; }
        
        public DependencyInjectionConfig()
        {
            Logging.Configure(Serilog.Events.LogEventLevel.Information);
            DirectoryHelper.EnsureCreated();
            
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider(validateScopes:true);
            RegisterTools(ServiceProvider.GetService<IToolFactory>());
        }

        public void ConfigureResources()
        {
            ResourceController.Load();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ApplicationSettingsService>();
            services.AddSingleton<IToolFactory, ToolFactory>();
            services.AddSingleton<PackFileDataBase>();
            services.AddSingleton<SkeletonAnimationLookUpHelper>();
            services.AddSingleton<CopyPasteManager>();
            services.AddSingleton<GameInformationFactory>();
            services.AddSingleton<PackFileService>();

            services.AddSingleton<GlobalEventSender>();
            services.AddSingleton<ScopeRepository>();
            services.AddScoped<EventHub>();
            services.AddScoped<SubToolWindowCreator>();
            

            services.AddScoped<MainWindow>();
            services.AddScoped<MainViewModel>();
            services.AddScoped<SettingsWindow>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<MenuBarViewModel>();

            foreach (var container in dependencyContainers)
                container.Register(services);

            TextEditor_DependencyInjectionContainer.Register(services);
            AnimMetaEditor_DependencyInjectionContainer.Register(services);
            AnimationEditors_DependencyInjectionContainer.Register(services);
            AnimationPack_DependencyInjectionContainer.Register(services);
            CampaignAnimBin_DependencyInjectionContainer.Register(services);
            VariantMeshDefinition_DependencyInjectionContainer.Register(services);
            AnimationFilePreviewEditor_DependencyInjectionContainer.Register(services);
            TextureEditor_DependencyInjectionContainer.Register(services);
            TwUi_DependencyInjectionContainer.Register(services);
            AudioEditor_DependencyInjectionContainer.Register(services);

            //AnimMetaDecoder_DependencyInjectionContainer.Register(services);
        }

        void RegisterTools(IToolFactory factory)
        {
            foreach (var container in dependencyContainers)
                container.RegisterTools(factory);

            TextEditor_DependencyInjectionContainer.RegisterTools(factory);
            AnimMetaEditor_DependencyInjectionContainer.RegisterTools(factory);
            AnimationEditors_DependencyInjectionContainer.RegisterTools(factory);
            AnimationPack_DependencyInjectionContainer.RegisterTools(factory);
            CampaignAnimBin_DependencyInjectionContainer.RegisterTools(factory);
            VariantMeshDefinition_DependencyInjectionContainer.RegisterTools(factory);
            AnimationFilePreviewEditor_DependencyInjectionContainer.RegisterTools(factory);
            TextureEditor_DependencyInjectionContainer.RegisterTools(factory);
            TwUi_DependencyInjectionContainer.RegisterTools(factory);
            AudioEditor_DependencyInjectionContainer.RegisterTools(factory);
            // AnimMetaDecoder_DependencyInjectionContainer.RegisterTools(factory);
        }

        public void ShowMainWindow()
        {
            using var scope = ServiceProvider.CreateScope();
            var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = scope.ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
    }
}
