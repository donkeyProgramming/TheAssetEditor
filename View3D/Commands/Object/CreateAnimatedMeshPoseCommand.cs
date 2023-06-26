using CommonControls.FileTypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class CreateAnimatedMeshPoseCommand : CommandBase<CreateAnimatedMeshPoseCommand>
    {
        List<MeshObject> _originalGeometries;

        List<Rmv2MeshNode> _meshNodes;
        AnimationFrame _frame;
        bool _convertToStaticFrame;

        public void Configure(List<Rmv2MeshNode> meshNodes, AnimationFrame frame, bool convertToStaticFrame = false)
        {
            _meshNodes = new List<Rmv2MeshNode>(meshNodes);
            _frame = frame;
            _convertToStaticFrame = convertToStaticFrame;
        }

        public override string GetHintText() => "Created static mesh from animation";

        protected override void ExecuteCommand()
        {
            _originalGeometries = new List<MeshObject>();
            foreach (var node in _meshNodes)
                _originalGeometries.Add(node.Geometry.Clone());

            foreach (var node in _meshNodes)
            {
                MeshAnimationHelper meshHelper = new MeshAnimationHelper(node, Matrix.Identity);

                for (int i = 0; i < node.Geometry.VertexCount(); i++)
                {
                    var vert = meshHelper.GetVertexTransform(_frame, i);
                    node.Geometry.TransformVertex(i, vert);
                }

                if (_convertToStaticFrame)
                    node.Geometry.ChangeVertexType(UiVertexFormat.Static, "");

                node.Geometry.RebuildVertexBuffer();
            }
        }

        protected override void UndoCommand()
        {
            for (int i = 0; i < _meshNodes.Count; i++)
                _meshNodes[i].Geometry = _originalGeometries[i];
        }
    }
}
