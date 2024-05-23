using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SkeletonEditor;
using SharedCore;
using SharedCore.Misc;
using SharedCore.PackFiles;
using SharedCore.ToolCreation;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class SkeletonTool_DevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public SkeletonTool_DevelopmentConfiguration(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => false;
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
