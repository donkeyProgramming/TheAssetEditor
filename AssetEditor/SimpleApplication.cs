using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AssetEditor
{
    public class SimpleApplication : IDisposable
    {
        public bool SkipLoadingWemFiles { get; set; } = true;
        IServiceScope _serviceScope;

        public SimpleApplication(bool loadAllCaFiles = true)
        {
            var serviceProvider = new DependencyInjectionConfig()
                  .Build();
            _serviceScope = serviceProvider.CreateScope();

            // Configure based on settings
            var settingsService = _serviceScope.ServiceProvider.GetService<ApplicationSettingsService>();
            settingsService.CurrentSettings.SkipLoadingWemFiles = SkipLoadingWemFiles;

            if (loadAllCaFiles)
            {
                var pfs = GetService<PackFileService>();
                pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            }
        }

        public T GetService<T>() => _serviceScope.ServiceProvider.GetService<T>();

        public void Dispose() => _serviceScope.Dispose();
    }
}
