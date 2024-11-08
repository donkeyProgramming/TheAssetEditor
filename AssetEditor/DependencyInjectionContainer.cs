using AssetEditor.DevConfigs.Base;
using AssetEditor.Services;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Editors.Shared.DevConfig.Base;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor
{
    internal class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<MainWindow>();
            serviceCollection.AddScoped<MainViewModel>();
            serviceCollection.AddScoped<IEditorCreator, EditorCreator>();

            serviceCollection.AddTransient<DeepSearchCommand>();
            serviceCollection.AddTransient<GenerateReportCommand>();
            serviceCollection.AddTransient<OpenGamePackCommand>();
            serviceCollection.AddTransient<OpenPackFileCommand>();
            serviceCollection.AddTransient<OpenSettingsDialogCommand>();
            serviceCollection.AddTransient<OpenWebpageCommand>();
            serviceCollection.AddTransient<OpenEditorCommand>();

            serviceCollection.AddTransient<SettingsWindow>();
            serviceCollection.AddScoped<SettingsViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();

            serviceCollection.AddScoped<MainWindow>();

            serviceCollection.AddScoped<IExceptionInformationProvider, CurrentEditorExceptionInfoProvider>();

            // Dev Config stuff
            serviceCollection.AddTransient<DevelopmentConfigurationManager>();
            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }
    }
}
