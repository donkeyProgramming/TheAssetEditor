using System.Linq;
using System.Reflection;
using CommonControls.Common;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.AnimationPack;
using CommonControls.Editors.CampaignAnimBin;
using CommonControls.Editors.TextEditor;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.Editors.Wtui;
using CommonControls.Events.Global;
using CommonControls.Events.UiCommands;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Resources;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;
using Monogame.WpfInterop.Common;

namespace CommonControls
{
    public class CommonControls_DependencyInjectionContainer : DependencyContainer
    {
        private readonly bool _loadResource;

        public CommonControls_DependencyInjectionContainer(bool loadResource = true)
        {
            _loadResource = loadResource;
        }

        public override void Register(IServiceCollection services)
        {
            Logging.Configure(Serilog.Events.LogEventLevel.Information);
            if (_loadResource) 
            { 
                ResourceController.Load();
                DirectoryHelper.EnsureCreated();
            }

            services.AddSingleton<ApplicationSettingsService>();
            services.AddSingleton<IToolFactory, ToolFactory>();
            services.AddSingleton<PackFileDataBase>();
            services.AddSingleton<SkeletonAnimationLookUpHelper>();
            services.AddSingleton<CopyPasteManager>();
            services.AddSingleton<GameInformationFactory>();
            services.AddSingleton<PackFileService>();
            services.AddSingleton<GlobalEventSender>();
            services.AddSingleton<ScopeRepository>();

            services.AddScoped<IUiCommandFactory, UiCommandFactory>();
            services.AddScoped<EventHub>();

            services.AddTransient<ImportAssetCommand>();
            services.AddTransient<ExportAssetCommand>(); // TODO: phazed added, ask ole is ok?

            // Editors that should be moved into their own projects
            TextEditor_DependencyInjectionContainer.Register(services);
            AnimationPack_DependencyInjectionContainer.Register(services);
            CampaignAnimBin_DependencyInjectionContainer.Register(services);
            VariantMeshDefinition_DependencyInjectionContainer.Register(services);
            AnimationFilePreviewEditor_DependencyInjectionContainer.Register(services);
            TwUi_DependencyInjectionContainer.Register(services);
        }

        public override void RegisterTools(IToolFactory factory)
        {
            TextEditor_DependencyInjectionContainer.RegisterTools(factory);
            AnimationPack_DependencyInjectionContainer.RegisterTools(factory);
            CampaignAnimBin_DependencyInjectionContainer.RegisterTools(factory);
            VariantMeshDefinition_DependencyInjectionContainer.RegisterTools(factory);
            AnimationFilePreviewEditor_DependencyInjectionContainer.RegisterTools(factory);
            TwUi_DependencyInjectionContainer.RegisterTools(factory);
        }
    }

    public class DependencyContainer
    {
        public virtual void Register(IServiceCollection serviceCollection) { }

        public virtual void RegisterTools(IToolFactory factory) { }

        protected void RegisterAllAsOriginalType<T>(IServiceCollection serviceCollection, ServiceLifetime scope)
        {
            var implementations = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .ToList();

            foreach (var implementation in implementations)
                    serviceCollection.Add(new ServiceDescriptor(implementation.UnderlyingSystemType, implementation, ServiceLifetime.Transient));
        }

        protected void RegisterAllAsInterface<T>(IServiceCollection serviceCollection, ServiceLifetime scope)
        {
            var implementations = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .ToList();

            foreach (var implementation in implementations)
                serviceCollection.Add(new ServiceDescriptor(typeof(T), implementation, ServiceLifetime.Transient));
        }
    }
}
