
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;
using SharedCore;
using SharedCore.Events;
using SharedCore.Events.Global;
using SharedCore.Misc;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;
using SharedCore.ToolCreation;

namespace Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        private readonly bool _loadResource;

        public DependencyInjectionContainer(bool loadResource = true)
        {
            _loadResource = loadResource;
        }

        public override void Register(IServiceCollection services)
        {
           // Logging.Configure(Serilog.Events.LogEventLevel.Information);
           // if (_loadResource)
           // {
           //     ResourceController.Load();
           //     DirectoryHelper.EnsureCreated();
           // }
           //
           // services.AddSingleton<ApplicationSettingsService>();
           // services.AddSingleton<IToolFactory, ToolFactory>();
           // services.AddSingleton<PackFileDataBase>();
           //
           // services.AddSingleton<CopyPasteManager>();
           // services.AddSingleton<GameInformationFactory>();
           // services.AddSingleton<PackFileService>();
           // services.AddSingleton<GlobalEventSender>();
           // services.AddSingleton<ScopeRepository>();
           //
           // services.AddScoped<IUiCommandFactory, UiCommandFactory>();
           // services.AddScoped<EventHub>();
           //
           // services.AddTransient<ImportAssetCommand>();
           //
           // services.AddTransient<IWindowFactory, WindowFactory>();
           // services.AddScoped<BoneMappingView>();
           // services.AddScoped<BoneMappingViewModel>();
           //
           // services.AddTransient<IPackFileUiProvider, PackFileUiProvider>();
           // services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();
           //
           // // Editors that should be moved into their own projects
           // TextEditor_DependencyInjectionContainer.Register(services);
           // VariantMeshDefinition_DependencyInjectionContainer.Register(services);
           // TwUi_DependencyInjectionContainer.Register(services);
        }

        public override void RegisterTools(IToolFactory factory)
        {
        }
    }
}
