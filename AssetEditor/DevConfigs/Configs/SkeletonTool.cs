using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.Shared.DevConfig.Configs
{
    internal class SkeletonTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly PackFileService _packFileService;

        public SkeletonTool(IEditorCreator editorCreator,  PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadWemFiles = false;

            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile("animations\\skeletons\\humanoid01.anim");
            _editorCreator.CreateFromFile(file, EditorEnums.Skeleton_Editor);
        }
    }
}
