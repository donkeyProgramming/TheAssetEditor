using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.AnimationMeta.DevConfig
{
    internal class AnimMetaTool : IDeveloperConfiguration
    {
        private readonly IEditorManager _editorCreator;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IPackFileService _packFileService;

        public AnimMetaTool(IEditorManager editorCreator, IPackFileContainerLoader packFileContainerLoader, IPackFileService packFileService)
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
  
            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, packFile, true);
            container!.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anm.meta");
            _editorCreator.CreateFromFile(file!);
        }
    }
}
