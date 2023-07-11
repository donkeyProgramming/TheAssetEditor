using CommonControls.Common;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolActions : NotifyPropertyChangedImpl
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public ToolActions(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void DivideSubMesh() => _uiCommandFactory.Create<DivideSubMeshCommand>();
        public void MergeObjects() => _uiCommandFactory.Create<MergeObjectsCommand>();
        public void DuplicateObject() => _uiCommandFactory.Create<DuplicateObjectCommand>();
        public void DeleteObject() => _uiCommandFactory.Create<DeleteObjectCommand>();
        public void ExpandFaceSelection() => _uiCommandFactory.Create<ExpandFaceSelectionCommand>();
        public void GroupItems() => _uiCommandFactory.Create<GroupItemsCommand>();
        public void ReduceMesh() => _uiCommandFactory.Create<ReduceMeshCommand>();
        public void CopyLod0ToEveryLods() => _uiCommandFactory.Create<CopyRootLodCommand>();
        public void CreateLods() => _uiCommandFactory.Create<CreateLodCommand>();
        public void ConvertFacesToVertex() => _uiCommandFactory.Create<ConvertFaceToObjectCommand>();
        public void ToggleShowSelection() => _uiCommandFactory.Create<ToggleViewSelectedCommand>();
        public void OpenBmiTool() => _uiCommandFactory.Create<OpenBmiToolCommand>();
        public void OpenSkeletonReshaperTool() => _uiCommandFactory.Create<OpenSkeletonReshaperToolCommand>();
        public void CreateStaticMeshes() => _uiCommandFactory.Create<CreateStaticMeshCommand>();
        public void PinMeshToMesh() => _uiCommandFactory.Create<OpenPinToolCommand>();
        public void OpenReRiggingTool() => _uiCommandFactory.Create<OpenReriggingToolCommand>();
        internal void UpdateWh2Model_ConvertAdditiveBlending() => _uiCommandFactory.Create<UpdateWh2TexturesCommand>(x=>x.Technique = Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.AdditiveBlending);
        internal void UpdateWh2Model_ConvertComparativeBlending() => _uiCommandFactory.Create<UpdateWh2TexturesCommand>(x => x.Technique = Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.ComparativeBlending);
        public void ShowVertexDebugInfo() => _uiCommandFactory.Create<OpenVertexDebuggerCommand>();
    }
}
