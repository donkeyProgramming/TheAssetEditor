using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.FastBin;

namespace Editors.Shared.DevConfig.Configs
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
            currentSettings.LoadWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"prefabs/campaign/chs_major_tze_05.bmd");
            new FastBinParser2().Load(file);
        }
    }
}
