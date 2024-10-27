using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.Shared.DevConfig.Configs
{
    internal class AnimMetaTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly PackFileService _packFileService;

        public AnimMetaTool(IEditorCreator editorCreator, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Throt.pack";
            _packFileService.Load(packFile, false, true);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anm.meta");
            _editorCreator.CreateFromFile(file);
        }
    }
}
