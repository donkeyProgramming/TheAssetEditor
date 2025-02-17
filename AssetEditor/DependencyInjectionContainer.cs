using AssetEditor.Services;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
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
            serviceCollection.AddSingleton<IEditorCreator>( x=> x.GetRequiredService<IEditorManager>());
            serviceCollection.AddSingleton<IEditorManager, EditorManager>();

            serviceCollection.AddTransient<OpenGamePackCommand>();
            serviceCollection.AddTransient<OpenPackFileCommand>();
            serviceCollection.AddTransient<OpenSettingsDialogCommand>();
            serviceCollection.AddTransient<OpenWebpageCommand>();
            serviceCollection.AddTransient<PrintScopesCommand>();
            serviceCollection.AddTransient<OpenEditorCommand>();
            serviceCollection.AddTransient<TogglePackFileExplorerCommand>();

            serviceCollection.AddTransient<SettingsWindow>();
            serviceCollection.AddScoped<SettingsViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();

            serviceCollection.AddScoped<MainWindow>();

            serviceCollection.AddSingleton<RecentFilesTracker>();

            serviceCollection.AddScoped<IExceptionInformationProvider, CurrentEditorExceptionInfoProvider>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }
    }
}
