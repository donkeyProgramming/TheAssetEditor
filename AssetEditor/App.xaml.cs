using AssetEditor.Services;
using CommonControls.Common;
using System.Windows;
using System.Windows.Threading;

namespace AssetEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        DependencyInjectionConfig _config;

        protected override void OnStartup(StartupEventArgs e)
        {
            VersionChecker.CheckVersion();
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(DispatcherUnhandledExceptionHandler);

            _config = new DependencyInjectionConfig();
            _config.ConfigureResources();
            _config.ShowMainWindow();
        }

        void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Logging.Create<App>().Here().Fatal(args.Exception.ToString());
            MessageBox.Show(args.Exception.ToString(), "Error");
            args.Handled = true;
        }
    }
}
