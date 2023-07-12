using AssetEditor.Services;
using AssetEditor.ViewModels;
using AssetEditor.Views;
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

            _serviceProvider = new DependencyInjectionConfig()
                .Build();
            _rootScope = _serviceProvider.CreateScope();
            ShowMainWindow();

            var settingsService = _rootScope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            if (settingsService.CurrentSettings.IsDeveloperRun)
            {
                var devConfig = _rootScope.ServiceProvider.GetRequiredService<DevelopmentConfiguration>();
                devConfig.CreateTestPackFiles();
                devConfig.OpenFileOnLoad();
            }
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
