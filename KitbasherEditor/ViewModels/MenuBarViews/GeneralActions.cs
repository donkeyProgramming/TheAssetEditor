using CommonControls.Common;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.UiCommands;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GeneralActions : NotifyPropertyChangedImpl
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public GeneralActions( IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void SortMeshes() => _uiCommandFactory.Create<SortMeshesCommand>();
        public void DeleteLods() => _uiCommandFactory.Create<DeleteLodsCommand>();
        public void Save() => _uiCommandFactory.Create<SaveCommand>();
        public void SaveAs() => _uiCommandFactory.Create<SaveAsCommand>();
        public void Undo() => _uiCommandFactory.Create<UndoCommand>();
        public void FocusSelection() => _uiCommandFactory.Create<UndoCommand>();
        public void ResetCamera() => _uiCommandFactory.Create<ResetCameraCommand>();
        public void ToggleBackFaceRendering() => _uiCommandFactory.Create<ToggleBackFaceRenderingCommand>();
        public void ToggleLargeSceneRendering() => _uiCommandFactory.Create<ToggleLargeSceneRenderingCommand>();
        public void GenerateWsModelWh3() => _uiCommandFactory.Create<GenerateWsModelCommand>(x=>x.GameFormat = CommonControls.Services.GameTypeEnum.Warhammer3);
        public void GenerateWsModelForWh2() => _uiCommandFactory.Create<GenerateWsModelCommand>(x => x.GameFormat = CommonControls.Services.GameTypeEnum.Warhammer2);
    }
}
