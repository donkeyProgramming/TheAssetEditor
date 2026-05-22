using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Commands.Object
{

    public class ObjectSelectionModeCommand : ICommand
    {
        SelectionManager _selectionManager;

        GeometrySelectionMode _newMode;
        ISelectable _selectedItem;
        ISelectionState _oldState;


        public string HintText { get => "Select Object"; }
        public bool IsMutation { get => false; }

        public void Configure(GeometrySelectionMode newMode)
        {
            _newMode = newMode;
        }

        public void Configure(ISelectable selectedItem, GeometrySelectionMode newMode)
        {
            _selectedItem = selectedItem;
            _newMode = newMode;
        }


        public ObjectSelectionModeCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var newSelectionState = _selectionManager.CreateSelectionSate(_newMode, _selectedItem);

            if (newSelectionState.Mode == GeometrySelectionMode.Object && _selectedItem != null)
                (newSelectionState as ObjectSelectionState).ModifySelectionSingleObject(_selectedItem, false);
            else if (newSelectionState.Mode == GeometrySelectionMode.Face)
                (newSelectionState as FaceSelectionState).RenderObject = _selectedItem;
            else if (newSelectionState.Mode == GeometrySelectionMode.Vertex)
                (newSelectionState as VertexSelectionState).RenderObject = _selectedItem;
            else if (newSelectionState.Mode == GeometrySelectionMode.Bone)
                (newSelectionState as BoneSelectionState).RenderObject = _selectedItem;
        }

        public void Undo()
        {
            _selectionManager.SetState(_oldState);
        }


    }
}
