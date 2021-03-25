using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Commands.Vertex
{

    public class TransformVertexCommand : CommandBase<TransformVertexCommand>
    {
        List<IGeometry> _geometryList;
        Vector3 _pivotPoint;
        List<int> _affectVertexes;
        bool _applyToNormals;
        public Matrix Transform { get; set; }

        SelectionManager _selectionManager;
        ISelectionState _oldSelectionState;

        public TransformVertexCommand(List<IGeometry> geometryList, Vector3 pivotPoint, bool applyToNormals = false, List<int> affectVertexes = null)
        {
            _geometryList = geometryList;
            _pivotPoint = pivotPoint;
            _applyToNormals = applyToNormals;
            _affectVertexes = affectVertexes;
        }

        public override string GetHintText()
        {
            return "Transform";
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
            var inv = Matrix.Invert(Transform);
            for(int i = 0; i < _geometryList.Count; i++)
            {
                var geo = _geometryList[i];
                if (_affectVertexes != null)
                {
                    for (int v = 0; v < _affectVertexes.Count; v++)
                        TransformVertex(geo, _affectVertexes[v], _pivotPoint, inv);
                }
                else
                {
                    for (int v = 0; v < geo.VertexCount(); v++)
                        TransformVertex(geo, v, _pivotPoint, inv);
                }

                geo.RebuildVertexBuffer();
            }

            _selectionManager.SetState(_oldSelectionState);
        }

        void TransformVertex(IGeometry geo, int vertedId, Vector3 transformOffset, Matrix undoTransform)
        {
            var vert = geo.GetVertexById(vertedId);
            vert -= transformOffset;
            vert = Vector3.Transform(vert, undoTransform);    // Rotate normal, bi normals and all that shit
            vert += transformOffset;
            geo.UpdateVertexPosition(vertedId, vert);
        }
    }
}
