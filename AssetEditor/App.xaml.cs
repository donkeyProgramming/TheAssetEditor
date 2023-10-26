using AssetEditor.DevelopmentConfiguration;
using AssetEditor.Services;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Threading;

namespace AssetEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        IServiceProvider _serviceProvider;
        IServiceScope _rootScope;

        protected override void OnStartup(StartupEventArgs e)
        {
            VersionChecker.CheckVersion();
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            _serviceProvider = new DependencyInjectionConfig().Build();
            _rootScope = _serviceProvider.CreateScope();

            // Show the settings window if its the first time the tool is ran
            var settingsService = _rootScope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = _rootScope.ServiceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = _rootScope.ServiceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }

            DevelopmentConfigurationManager devConfigManager = null;
            if (settingsService.CurrentSettings.IsDeveloperRun)
                devConfigManager = _rootScope.ServiceProvider.GetRequiredService<DevelopmentConfigurationManager>();

            devConfigManager?.OverrideSettings();        

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


                    //// DEBUGGINg CODE: BEGIN
                    //var pathModel = @"variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/emp_karl_franz.rigid_model_v2";
                    //var packFile = packfileService.FindFile(pathModel);
                    //var packFileContainer = packfileService.GetPackFileContainer(packFile);

                    //var exporterCommandFactory = _rootScope.ServiceProvider.GetService<IUiCommandFactory>();
                    //exporterCommandFactory.Create<ExportAssetCommand>().Execute(packFileContainer, pathModel);
                    // DEBUGGINg CODE: END
                    // TODO: enable for local debuggin
//                    packfileService.Load(@"C:\temp\TestCustomPackFile.pack");
                }
            }

            devConfigManager?.CreateTestPackFiles();
            devConfigManager?.OpenFileOnLoad();

           

            ShowMainWindow();
        }

        void ShowMainWindow()
        {
            var mainWindow = _rootScope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _rootScope.ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }

        void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Logging.Create<App>().Here().Fatal(args.Exception.ToString());
            MessageBox.Show(args.Exception.ToString(), "Error");
            args.Handled = true;
        }
    }
}
