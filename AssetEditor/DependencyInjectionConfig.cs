using AssetEditor.Services;
using AssetEditor.ViewModels;
using AssetEditor.Views;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Resources;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetEditor
{
    class DependencyInjectionConfig
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public DependencyInjectionConfig()
        {
            Logging.Configure(Serilog.Events.LogEventLevel.Information);
            ResourceController.Load();
            GameInformationFactory.Create();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ApplicationSettingsService>();
            services.AddSingleton<ToolFactory>();
            services.AddSingleton<FileTypes.PackFiles.Models.PackFileDataBase>();

            services.AddTransient<GameInformationService>();
            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<MenuBarViewModel>();
            services.AddTransient<FileTypes.PackFiles.Services.PackFileService>();
        }

        public void ShowMainWindow()
        {
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
    }

}
