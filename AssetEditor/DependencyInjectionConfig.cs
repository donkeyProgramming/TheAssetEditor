using AnimationEditor;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Audio;
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.AnimationPack;
using CommonControls.Editors.AnimMeta;
using CommonControls.Editors.CampaignAnimBin;
using CommonControls.Editors.TextEditor;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.Editors.Wtui;
using CommonControls.Events.Global;
using CommonControls.Events.UiCommands;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Resources;
using CommonControls.Services;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using System;
using TextureEditor;
using View3D;

namespace AssetEditor
{
    public class DependencyInjectionConfig
    {
        DependencyContainer[] dependencyContainers = new DependencyContainer[]
        {
            new View3D_DependencyContainer(),
            new KitbasherEditor_DependencyInjectionContainer(),
        };

        public DependencyInjectionConfig()
        {
            Logging.Configure(Serilog.Events.LogEventLevel.Information);
            DirectoryHelper.EnsureCreated();
            MetaDataTagDeSerializer.EnsureMappingTableCreated();
        }

        public IServiceProvider Build()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            RegisterTools(_serviceProvider.GetService<IToolFactory>());
            return _serviceProvider;
        }

        public DependencyInjectionConfig ConfigureResources()
        {
            ResourceController.Load();
            return this;
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
            services.AddScoped<IEditorCreator, EditorCreator>();
            services.AddScoped<IUiCommandFactory, UiCommandFactory>();

            services.AddTransient<OpenEditorCommand>();
            services.AddTransient<OpenFileInEditorCommand>();



            services.AddScoped<SettingsWindow>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<MenuBarViewModel>();

            services.AddTransient<DevelopmentConfiguration>();


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
    }
}
