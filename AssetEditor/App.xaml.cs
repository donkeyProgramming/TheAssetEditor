using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using AssetEditor.Services;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor
{
    public partial class App : Application
    {
        IServiceProvider _serviceProvider;
        IServiceScope _rootScope;

        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationStateRecorder.Initialize();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            VersionChecker.CheckVersion();
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            var forceValidateServiceScopes = Debugger.IsAttached;
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes);
            _rootScope = _serviceProvider.CreateScope();

            _ = _rootScope.ServiceProvider.GetRequiredService<RecentFilesTracker>(); // Force instance
            var scopeRepo = _rootScope.ServiceProvider.GetRequiredService<ScopeRepository>();
            scopeRepo.Root = _rootScope;

            var settingsService = _rootScope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            settingsService.AllowSettingsUpdate = true;
            settingsService.Load();

            // Init 3d world
            var gameWorld = _rootScope.ServiceProvider.GetRequiredService<IWpfGame>();
            gameWorld.ForceEnsureCreated();

            // Show the settings window if its the first time the tool is ran
            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = _rootScope.ServiceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = _rootScope.ServiceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }

            var devConfigManager = _rootScope.ServiceProvider.GetRequiredService<DevelopmentConfigurationManager>();
            devConfigManager.Initialize(e);
            devConfigManager.OverrideSettings();

            // Load all packfiles
            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
            {
                var gamePath = settingsService.GetGamePathForCurrentGame();
                if (gamePath != null)
                {
                    var packfileService = _rootScope.ServiceProvider.GetRequiredService<IPackFileService>();
                    var containerLoader = _rootScope.ServiceProvider.GetRequiredService<IPackFileContainerLoader>();
                    var loadRes = containerLoader.LoadAllCaFiles(settingsService.CurrentSettings.CurrentGame);

                    if (loadRes == null)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                    else
                        packfileService.AddContainer(loadRes);
                }
            }

            devConfigManager.CreateTestPackFiles();
            devConfigManager.OpenFileOnLoad();

            ShowMainWindow();
        }

        void ShowMainWindow()
        {
            var applicationSettingsService = _rootScope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            ThemesController.SetTheme(applicationSettingsService.CurrentSettings.Theme);

            var mainWindow = _rootScope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _rootScope.ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();

            if (applicationSettingsService.CurrentSettings.StartMaximised == true)
                mainWindow.WindowState = WindowState.Maximized;
        }

        void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Logging.Create<App>().Here().Fatal(args.Exception.ToString());

            var exceptionService = _rootScope?.ServiceProvider.GetService<IExceptionService>();
            if (exceptionService != null)
               exceptionService.CreateDialog(args.Exception);   
            else
                MessageBox.Show(args.Exception.ToString(), "Error");

            args.Handled = true;
        }
    }
}
