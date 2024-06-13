using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<ApplicationSettingsService>();
            services.AddSingleton<IToolFactory, ToolFactory>();
            services.AddSingleton<PackFileDataBase>();

            services.AddSingleton<CopyPasteManager>();
            services.AddSingleton<GameInformationFactory>();
            services.AddSingleton<PackFileService>();
            services.AddSingleton<GlobalEventSender>();
            services.AddSingleton<ScopeRepository>();
            services.AddSingleton<TouchedFilesRecorder>();

            services.AddScoped<IUiCommandFactory, UiCommandFactory>();
            services.AddScoped<EventHub>();
        }
    }
}
