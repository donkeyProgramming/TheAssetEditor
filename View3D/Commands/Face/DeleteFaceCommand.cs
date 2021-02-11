using Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Commands.Face
{

    public class DeleteFaceCommand : CommandBase<DeleteFaceCommand>
    {
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        IGeometry _oldGeometry;

        List<int> _facesToDelete;
        IGeometry _geo;

        public DeleteFaceCommand(IGeometry geoObject, List<int> facesToDelete)
        {
            _facesToDelete = facesToDelete;
            _geo = geoObject;
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();

            _oldGeometry = _geo.Clone();
            _geo.RemoveFaces(_facesToDelete);

            var faceSelectionState = _selectionManager.GetState() as FaceSelectionState;
            faceSelectionState.Clear();
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
            var faceSelectionState = _selectionManager.GetState() as FaceSelectionState;
            faceSelectionState.RenderObject.Geometry = _oldGeometry;
        }
    }
}
