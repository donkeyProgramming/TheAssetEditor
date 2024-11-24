using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using System;
using System.Diagnostics;

namespace AssetEditor.Services
{
    //public class SimpleApplication : IDisposable
    //{
    //    public bool LoadWemFiles { get; set; } = true;
    //    private readonly IServiceScope _serviceScope;
    //
    //    public SimpleApplication(bool loadAllCaFiles = true)
    //    {
    //        var forceValidateServiceScopes = Debugger.IsAttached;
    //        var serviceProvider = new DependencyInjectionConfig().Build(forceValidateServiceScopes);
    //        _serviceScope = serviceProvider.CreateScope();
    //
    //        // Configure based on settings
    //        var settingsService = _serviceScope.ServiceProvider.GetService<ApplicationSettingsService>();
    //        settingsService.CurrentSettings.LoadWemFiles = LoadWemFiles;
    //
    //        if (loadAllCaFiles)
    //        {
    //            var pfs = GetService<PackFileService>();
    //            pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
    //        }
    //    }
    //
    //    public T GetService<T>() => _serviceScope.ServiceProvider.GetService<T>();
    //
    //    public void Dispose() => _serviceScope.Dispose();
    //}
}
