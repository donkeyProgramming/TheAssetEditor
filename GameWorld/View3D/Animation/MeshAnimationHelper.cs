using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Animation
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
            var geo = _mesh.Geometry as MeshObject;
            var vert = geo.GetVertexExtented(vertexId);
            var m = GetVertexTransform(frame, vertexId);
            var finalTransfrom = Matrix.CreateTranslation(new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z)) * m;
            return finalTransfrom;
        }

        public Matrix GetVertexTransform(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as MeshObject;
            var vert = geo.GetVertexExtented(vertexId);

            var blendIndex = new int[4] { (int)vert.BlendIndices.X, (int)vert.BlendIndices.Y, (int)vert.BlendIndices.Z, (int)vert.BlendIndices.W };
            var blendWeight = new float[4] { vert.BlendWeights.X, vert.BlendWeights.Y, vert.BlendWeights.Z, vert.BlendWeights.W };

            var transformSum = new Matrix();
            for (var i = 0; i < geo.WeightCount; i++)
            {
                var simpleMatrix = frame.BoneTransforms[blendIndex[i]].WorldTransform;
                transformSum += simpleMatrix * blendWeight[i];
            }

            var result = transformSum * _worldTransform;
            return result;
        }
    }
}
