using System.Windows;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Shared.Core.DevConfig
{
    public class DevelopmentConfigurationManager
    {
        private readonly ILogger _logger = Logging.Create<DevelopmentConfigurationManager>();
        private readonly IPackFileService _packFileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IEnumerable<IDeveloperConfiguration> _developerConfigurations;
        private IDeveloperConfiguration _activeConfig;

        public DevelopmentConfigurationManager(IEnumerable<IDeveloperConfiguration> developerConfigurations, IPackFileService packFileService, ApplicationSettingsService settingsService)
        {
            _developerConfigurations = developerConfigurations;
            _packFileService = packFileService;
            _settingsService = settingsService;
        }

        public void CreateTestPackFiles()
        {
            if (_activeConfig != null)
            {
                var newPackFile = _packFileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
                _packFileService.SetEditablePack(newPackFile);
            }
        }

        public void OpenFileOnLoad() => _activeConfig?.OpenFileOnLoad();

        public void OverrideSettings() => _activeConfig?.OverrideSettings(_settingsService.CurrentSettings);

        public void Initialize(StartupEventArgs e)
        {
            var cfgName = GetDevCfg(e);
            if (cfgName == null)
                return;

            var selectedCfg = _developerConfigurations
                .Where(x => x.GetType().Name.ToLower() == cfgName)
                .FirstOrDefault();

            if (selectedCfg == null)
            {
                _logger.Here().Error($"DevCfg '{e.Args[1]}' not found. Possible values are:");
                foreach (var cfg in _developerConfigurations)
                    _logger.Here().Error(cfg.GetType().Name);

                return;
            }

            _activeConfig = selectedCfg;
            _logger.Here().Information($"Dev cfg {_activeConfig.GetType().Name} selected. Settings changed not updated to file");

            _settingsService.AllowSettingsUpdate = false;
        }

        static string? GetDevCfg(StartupEventArgs args)
        {
            if (args.Args.Length != 2)
                return null;
            if (args.Args[0].ToLower().Trim() == "-devcfg")
                return args.Args[1].ToLower().Trim();

            return null;
        }
    }
}
