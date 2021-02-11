using Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands.Face
{
    public class FaceSelectionCommand : CommandBase<FaceSelectionCommand>
    {
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        bool _isModification;
        List<int> _selectedFaces;

        public FaceSelectionCommand(List<int> selectedFaces, bool isModification = false)
        {
            _selectedFaces = selectedFaces;
            _isModification = isModification;
        }

        public FaceSelectionCommand(int selectedFace, bool isModification = false)
        {
            _selectedFaces = new List<int>() { selectedFace };
            _isModification = isModification;
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as FaceSelectionState;
            _logger.Here().Information($"Command info - Mod[{_isModification}] Item[{currentState.RenderObject.Name}] faces[{_selectedFaces.Count}]");

            if (!_isModification)
                currentState.Clear();

            foreach (var newSelectionItem in _selectedFaces)
                currentState.ModifySelection(newSelectionItem);

            currentState.EnsureSorted();
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}
