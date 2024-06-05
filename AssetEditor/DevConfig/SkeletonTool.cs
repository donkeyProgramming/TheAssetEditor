using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SkeletonEditor;
using AssetEditor.DevConfigs.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace AssetEditor.DevConfig
{
    internal class SkeletonTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public SkeletonTool(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            var editorView = _toolFactory.Create<EditorHost<SkeletonEditorViewModel>>();
            _editorCreator.CreateEmptyEditor(editorView);
        }
    }
}
