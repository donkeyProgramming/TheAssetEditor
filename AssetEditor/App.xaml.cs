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
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor
{
    public partial class App : Application
    {
        IServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationStateRecorder.Initialize();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            VersionChecker.CheckVersion();
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            var forceValidateServiceScopes = Debugger.IsAttached;
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes);

            _ = _serviceProvider.GetRequiredService<RecentFilesTracker>(); // Force instance of the RecentFilesTracker
            _ = _serviceProvider.GetRequiredService<IScopeRepository>();  // Force instance of the IScopeRepository

            var settingsService = _serviceProvider.GetRequiredService<ApplicationSettingsService>();
            settingsService.AllowSettingsUpdate = true;
            settingsService.Load();


            // Show the settings window if its the first time the tool is ran
            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }

            var devConfigManager = _serviceProvider.GetRequiredService<DevelopmentConfigurationManager>();
            devConfigManager.Initialize(e);
            devConfigManager.OverrideSettings();

            // Load all packfiles
            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
            {
                var gamePath = settingsService.GetGamePathForCurrentGame();
                if (gamePath != null)
                {
                    var packfileService = _serviceProvider.GetRequiredService<IPackFileService>();
                    var containerLoader = _serviceProvider.GetRequiredService<IPackFileContainerLoader>();
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
            var applicationSettingsService = _serviceProvider.GetRequiredService<ApplicationSettingsService>();
            ThemesController.SetTheme(applicationSettingsService.CurrentSettings.Theme);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Closed += OnMainWindowClosed;
            mainWindow.Show();

            // Ensure the window doesn't cover up the windows bar.
            mainWindow.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            mainWindow.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            if (applicationSettingsService.CurrentSettings.StartMaximised == true)
                SystemCommands.MaximizeWindow(mainWindow);
        }

        private void OnMainWindowClosed(object sender, EventArgs e)
        {
            foreach (Window window in Current.Windows)
                window.Close();
            Shutdown();
        }

        void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Logging.Create<App>().Here().Fatal(args.Exception.ToString());

            var exceptionService = _serviceProvider?.GetService<IStandardDialogs>();
            if (exceptionService != null)
               exceptionService.ShowExceptionWindow(args.Exception);   
            else
                MessageBox.Show(args.Exception.ToString(), "Error");

            args.Handled = true;
        }
    }
}
