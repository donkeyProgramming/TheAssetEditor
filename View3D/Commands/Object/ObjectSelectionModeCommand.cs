using MonoGame.Framework.WpfInterop;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{

    public class ObjectSelectionModeCommand : CommandBase<ObjectSelectionModeCommand>
    {
        SelectionManager _selectionManager;

        GeometrySelectionMode _newMode;
        ISelectable _selectedItem;
        ISelectionState _oldState;

        public ObjectSelectionModeCommand(GeometrySelectionMode newMode)
        {
            _newMode = newMode;
           
        }

        public ObjectSelectionModeCommand(ISelectable selectedItem, GeometrySelectionMode newMode) : this(newMode)
        {
            _selectedItem = selectedItem;
        }

        public override string GetHintText()
        {
            return "Select Object";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var newSelectionState = _selectionManager.CreateSelectionSate(_newMode, _selectedItem);

            if (newSelectionState.Mode == GeometrySelectionMode.Object && _selectedItem != null)
                (newSelectionState as ObjectSelectionState).ModifySelectionSingleObject(_selectedItem, false);
            else if (newSelectionState.Mode == GeometrySelectionMode.Face)
                (newSelectionState as FaceSelectionState).RenderObject = _selectedItem;
            else if(newSelectionState.Mode == GeometrySelectionMode.Vertex)
                (newSelectionState as VertexSelectionState).RenderObject = _selectedItem;
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }

        public override bool IsMutation() => false;
    }
}
