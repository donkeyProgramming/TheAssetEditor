using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Animation
{
    public class MeshAnimationHelper
    {
        Rmv2MeshNode _mesh;
        Matrix _worldTransform;
        public MeshAnimationHelper(Rmv2MeshNode mesh, Matrix worldTransform)
        {
            _mesh = mesh;
            _worldTransform = worldTransform;
        }

        public Matrix GetVertexTransformWorld(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Geometry;
            var vert = geo.GetVertexExtented(vertexId);
            var m = GetVertexTransform(frame, vertexId);
            Matrix finalTransfrom = Matrix.CreateTranslation(new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z)) * m;
            return finalTransfrom;
        }

        public Matrix GetVertexTransform(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Geometry;
            var vert = geo.GetVertexExtented(vertexId);

            var blendIndex = new int[4] { (int)vert.BlendIndices.X, (int)vert.BlendIndices.Y, (int)vert.BlendIndices.Z, (int)vert.BlendIndices.W };
            var blendWeight = new float[4] { vert.BlendWeights.X, vert.BlendWeights.Y, vert.BlendWeights.Z, vert.BlendWeights.W };

            var transformSum = new Matrix();
            for (int i = 0; i < geo.WeightCount; i++)
            {
                var simpleMatrix = frame.BoneTransforms[blendIndex[i]].WorldTransform;
                transformSum += simpleMatrix * blendWeight[i];
            }

            var result = transformSum * _worldTransform;
            return result;
        }
    }
}
