using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands.Vertex
{
    public class VertexSelectionCommand : CommandBase<VertexSelectionCommand>
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        bool _isAdd;
        bool _isRemove;
        List<int> _selectedVertices;

        public VertexSelectionCommand(List<int> selectedVertices, bool isAdd, bool isRemove)
        {
            _selectedVertices = selectedVertices;
            _isAdd = isAdd;
            _isRemove = isRemove;
        }

        public override string GetHintText()
        {
            return "Select Vertex";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as VertexSelectionState;
            _logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] Vertices[{_selectedVertices.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedVertices, _isRemove);

            currentState.EnsureSorted();
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}