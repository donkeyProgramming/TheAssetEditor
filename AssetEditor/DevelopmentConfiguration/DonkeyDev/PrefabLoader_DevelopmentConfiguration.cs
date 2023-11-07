using CommonControls.FileTypes.FastBin;
using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class PrefabLoader_DevelopmentConfiguration : DonkeyConfigurationBase<PrefabLoader_DevelopmentConfiguration>
    {
        private readonly PackFileService _packFileService;

        public PrefabLoader_DevelopmentConfiguration(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public override void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public override void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"prefabs/campaign/chs_major_tze_05.bmd");
            new FastBinParser2().Load(file);
        }
    }
}
