using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
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
        List<MeshObject> _geometryList;
        public Vector3 PivotPoint;
        public Matrix Transform { get; set; }
        public bool InvertWindingOrder { get; set; } = false;

        SelectionManager _selectionManager;
        ISelectionState _oldSelectionState;

        public TransformVertexCommand(List<MeshObject> geometryList, Vector3 pivotPoint)
        {
            _geometryList = geometryList;
            PivotPoint = pivotPoint;
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
            Transform.Decompose(out var scale, out var rot, out var trans);

            for (int meshIndex = 0; meshIndex < _geometryList.Count; meshIndex++)
            {
                var geo = _geometryList[meshIndex];
                if (_oldSelectionState.Mode == GeometrySelectionMode.Vertex)
                {
                    var vState = _oldSelectionState as VertexSelectionState;
                    for (int vertIndex = 0; vertIndex < vState.VertexWeights.Count; vertIndex++)
                    {
                        if (vState.VertexWeights[vertIndex] != 0)
                        {
                            var weight = vState.VertexWeights[vertIndex];
                            var vertexScale = Vector3.Lerp(Vector3.One, scale, weight);
                            var vertRot = Quaternion.Slerp(Quaternion.Identity, rot, weight);
                            var vertTrnas = trans * weight;

                            var weightedUndoTransform = Matrix.CreateScale(vertexScale) * Matrix.CreateFromQuaternion(vertRot) * Matrix.CreateTranslation(vertTrnas);
                            var finalMatrix = Matrix.CreateTranslation(-PivotPoint) * Matrix.Invert(weightedUndoTransform) * Matrix.CreateTranslation(PivotPoint);

                            geo.TransformVertex(vertIndex, finalMatrix);
                        }
                    }
                }
                else
                {
                    var undoMatrix = Matrix.CreateTranslation(-PivotPoint) * Matrix.Invert(Transform) * Matrix.CreateTranslation(PivotPoint);
                    for (int v = 0; v < geo.VertexCount(); v++)
                        geo.TransformVertex(v, undoMatrix);

                    if (InvertWindingOrder)
                    {
                        var indexes = geo.GetIndexBuffer();
                        for (int index = 0; index < indexes.Count; index += 3)
                        {
                            var temp = indexes[index + 2];
                            indexes[index + 2] = indexes[index + 0];
                            indexes[index + 0] = temp;
                        }
                        geo.SetIndexBuffer(indexes);
                    }
                }

                geo.RebuildVertexBuffer();
            }

            _selectionManager.SetState(_oldSelectionState);
        }

    }
}
