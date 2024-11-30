using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.AnimationMeta.DevConfig
{
    internal class AnimMetaTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IPackFileService _packFileService;

        public AnimMetaTool(IEditorCreator editorCreator, IPackFileContainerLoader packFileContainerLoader, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileContainerLoader = packFileContainerLoader;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Throt.pack";
  
            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anm.meta");
            _editorCreator.CreateFromFile(file);
        }
    }
}
