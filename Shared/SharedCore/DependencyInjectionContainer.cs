using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
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
            services.AddSingleton<IEditorDatabase, EditorDatabase>();
            services.AddSingleton<CopyPasteManager>();
            services.AddSingleton<IPackFileService, PackFileService>();
            services.AddScoped<IFileSaveService, FileSaveService>();
            services.AddScoped<ScopeToken>();
            services.AddSingleton<IScopeRepository, ScopeRepository>();
            services.AddSingleton<TouchedFilesRecorder>();
            services.AddScoped<IUiCommandFactory, UiCommandFactory>();
            services.AddScoped<IEventHub, LocalScopeEventHub>();
            services.AddSingleton<IGlobalEventHub, SingletonScopeEventHub>();
            services.AddScoped<IExceptionService, ExceptionService>();
            services.AddScoped<IExceptionInformationProvider, BasicExceptionInformationProvider>();
            services.AddTransient<DevelopmentConfigurationManager>();

            services.AddSingleton<IPackFileContainerLoader, PackFileContainerLoader>();
        }
    }

}

