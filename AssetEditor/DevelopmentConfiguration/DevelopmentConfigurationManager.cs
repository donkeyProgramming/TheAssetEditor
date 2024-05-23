using SharedCore;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetEditor.DevelopmentConfiguration
{
    public class DevelopmentConfigurationManager
    {
        private readonly PackFileService _packFileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IEnumerable<IDeveloperConfiguration> _developerConfigurations;

        public DevelopmentConfigurationManager(IEnumerable<IDeveloperConfiguration> developerConfigurations, PackFileService packFileService, ApplicationSettingsService settingsService)
        {
            _developerConfigurations = developerConfigurations;
            _packFileService = packFileService;
            _settingsService = settingsService;
        }

        public void CreateTestPackFiles()
        {
            var newPackFile = _packFileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            _packFileService.SetEditablePack(newPackFile);
        }

        public void OpenFileOnLoad() => GetCurrentConfig()?.OpenFileOnLoad();

        public void OverrideSettings() => GetCurrentConfig()?.OverrideSettings(_settingsService.CurrentSettings);

        IDeveloperConfiguration GetCurrentConfig()
        {
            var machineName = Environment.MachineName;
            var devConfig = _developerConfigurations
                .Where(x => x.IsEnabled)
                .Where(x => x.MachineNames.Contains(machineName))
                .ToList();

            if (devConfig.Count >= 2)
                throw new Exception($"Multiple IDeveloperConfigurations enabled for computer {machineName}");
            return devConfig.FirstOrDefault();
        }
    }
}
