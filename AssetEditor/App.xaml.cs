using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AssetEditor.Services;
using AssetEditor.UiCommands;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using CommunityToolkit.Diagnostics;
using Editors.Ipc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.Common;

namespace AssetEditor
{
    public partial class App : Application, IAssetEditorMain
    {
        IServiceProvider? _serviceProvider;
        AssetEditorIpcServer? _ipcServer;

        public IServiceProvider ServiceProvider 
        {
            get 
            {
                Guard.IsNotNull(_serviceProvider, nameof(ServiceProvider));
                return _serviceProvider;
            } 
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationStateRecorder.Initialize();
            PackFileLog.IsLoggingEnabled = false;

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            var forceValidateServiceScopes = Debugger.IsAttached;
            _serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes);

            _ = _serviceProvider.GetRequiredService<RecentFilesTracker>(); // Force instance of the RecentFilesTracker
            _ = _serviceProvider.GetRequiredService<IScopeRepository>();  // Force instance of the IScopeRepository

            var uiCommandFactory = _serviceProvider.GetRequiredService<IUiCommandFactory>();

            var settingsService = _serviceProvider.GetRequiredService<ApplicationSettingsService>();
            settingsService.AllowSettingsUpdate = true;
            settingsService.Load();

            var localizationManager = _serviceProvider.GetRequiredService<LocalizationManager>();
            localizationManager.GetPossibleLanguages();
            localizationManager.LoadLanguage("en");

            // Show the settings window if its the first time the tool is ran
            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
                HandleFirstTimeSettings(uiCommandFactory, settingsService);

            var devConfigManager = _serviceProvider.GetRequiredService<DevelopmentConfigurationManager>();
            devConfigManager.Initialize(e);
            devConfigManager.OverrideSettings();

            // Load all packfiles
            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
                LoadCAPackFiles(settingsService);

            devConfigManager.CreateTestPackFiles();
            devConfigManager.OpenFileOnLoad();

            ShowMainWindow();

            _ipcServer = _serviceProvider.GetRequiredService<AssetEditorIpcServer>();
            _ipcServer.Start();
            _ = CheckVersion(uiCommandFactory);
        }

        private static void HandleFirstTimeSettings(IUiCommandFactory uiCommandFactory, ApplicationSettingsService settingsService)
        {
            uiCommandFactory.Create<OpenSettingsDialogCommand>().Execute();

            settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
            settingsService.Save();
        }

        private void LoadCAPackFiles(ApplicationSettingsService settingsService)
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

        void ShowMainWindow()
        {
            var applicationSettingsService = _serviceProvider.GetRequiredService<ApplicationSettingsService>();
            ThemesController.SetTheme(applicationSettingsService.CurrentSettings.Theme);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Closed += OnMainWindowClosed;
            mainWindow.Show();

            // Ensure the window doesn't cover up the windows bar
            mainWindow.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            mainWindow.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            if (applicationSettingsService.CurrentSettings.StartMaximised == true)
                SystemCommands.MaximizeWindow(mainWindow);
        }

       private void OnMainWindowClosed(object sender, EventArgs e)
        {
            _ipcServer?.Dispose();
            _ipcServer = null;

            foreach (Window window in Current.Windows)
                window.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ipcServer?.Dispose();
            _ipcServer = null;
            base.OnExit(e);
        }


        private static async Task CheckVersion(IUiCommandFactory uiCommandFactory)
        {
            var newerReleases = await VersionChecker.GetNewerReleases();
            if (newerReleases != null)
                uiCommandFactory.Create<OpenUpdaterWindowCommand>().Execute(newerReleases);
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
