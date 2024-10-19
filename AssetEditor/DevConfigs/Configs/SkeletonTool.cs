using AnimationEditor.SkeletonEditor;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Editors.Shared.DevConfig.Configs
{
    internal class SkeletonTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IEditorDatabase _toolFactory;
        private readonly PackFileService _packFileService;

        public SkeletonTool(IEditorCreator editorCreator, IEditorDatabase toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
           // var editorView = _toolFactory.Create<EditorHost<SkeletonEditorViewModel>>();
           // _editorCreator.Create(editorView);
        }
    }
}
