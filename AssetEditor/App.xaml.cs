using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using AssetEditor.Services;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Editors.Shared.DevConfig.Base;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ErrorHandling;
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
            VersionChecker.CheckVersion();
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            var forceValidateServiceScopes = Debugger.IsAttached;
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes);
            _rootScope = _serviceProvider.CreateScope();

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
                    var gameInformationFactory = _rootScope.ServiceProvider.GetRequiredService<GameInformationFactory>();
                    var packfileService = _rootScope.ServiceProvider.GetRequiredService<PackFileService>();
                    var gameName = gameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName;
                    var loadRes = packfileService.LoadAllCaFiles(gamePath, gameName);
                    if (!loadRes)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                }
            }

            devConfigManager.CreateTestPackFiles();
            devConfigManager.OpenFileOnLoad();

            ShowMainWindow();
        }

        void ShowMainWindow()
        {
            var mainWindow = _rootScope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _rootScope.ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();

            var applicationSettingsService = _rootScope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            ThemesController.SetTheme(applicationSettingsService.CurrentSettings.Theme);
        }

        void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Logging.Create<App>().Here().Fatal(args.Exception.ToString());
            MessageBox.Show(args.Exception.ToString(), "Error");
            args.Handled = true;
        }
    }
}
