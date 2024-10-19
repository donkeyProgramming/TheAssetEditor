using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

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
            currentSettings.LoadWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            _editorCreator.Create(EditorEnums.Skeleton_Editor);
        }
    }
}
