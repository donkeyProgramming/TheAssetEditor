using AssetEditor.Services;
using AssetEditor.Services.Ipc;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using AssetEditor.Views.Updater;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Events.Global;
using Shared.Core.ToolCreation;

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
            serviceCollection.AddTransient<OpenUpdaterWindowCommand>();
            serviceCollection.AddTransient<OpenWebpageCommand>();
            serviceCollection.AddTransient<PrintScopesCommand>();
            serviceCollection.AddTransient<OpenEditorCommand>();
            serviceCollection.AddTransient<TogglePackFileExplorerCommand>();
            serviceCollection.AddTransient<IExternalPackFileLookup, ExternalPackFileLookup>();
            serviceCollection.AddTransient<IExternalPackLoader, ExternalPackLoader>();
            serviceCollection.AddTransient<IIpcUserNotifier, IpcUserNotifier>();
            serviceCollection.AddTransient<IExternalFileOpenExecutor, ExternalFileOpenExecutor>();
            serviceCollection.AddTransient<IIpcRequestHandler, IpcRequestHandler>();
            serviceCollection.AddSingleton<AssetEditorIpcServer>();

            serviceCollection.AddTransient<SettingsWindow>();
            serviceCollection.AddTransient<SettingsViewModel>();
            serviceCollection.AddTransient<UpdaterWindow>();
            serviceCollection.AddTransient<UpdaterViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();

            serviceCollection.AddScoped<MainWindow>();

            serviceCollection.AddSingleton<RecentFilesTracker>();

            serviceCollection.AddScoped<IExceptionInformationProvider, CurrentEditorExceptionInfoProvider>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }
    }
}
