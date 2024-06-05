using AssetEditor.DevConfigs.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.FastBin;

namespace AssetEditor.DevConfig
{
    internal class PrefabTesting : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;

        public PrefabTesting(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"prefabs/campaign/chs_major_tze_05.bmd");
            new FastBinParser2().Load(file);
        }
    }
}
