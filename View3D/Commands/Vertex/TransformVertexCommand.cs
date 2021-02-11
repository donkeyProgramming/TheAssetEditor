using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;

namespace View3D.Commands.Vertex
{
    class TransformVertexCommand : CommandBase<VertexSelectionCommand>
    {
        ISelectable _node;
        IGeometry _oldGeo;

        SelectionManager _selectionManager;
        ISelectionState _oldSelectionState;
        public TransformVertexCommand(ISelectable node)
        {
            _node = node;
            _oldGeo = _node.Geometry.Clone();
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldSelectionState = _selectionManager.GetStateCopy();
            // Nothing to do, vertexes already updated
        }

        protected override void UndoCommand()
        {
            _node.Geometry = _oldGeo;
            _selectionManager.SetState(_oldSelectionState);
        }
    }
}
