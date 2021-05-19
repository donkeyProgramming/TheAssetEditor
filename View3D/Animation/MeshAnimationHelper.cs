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

        public Matrix GetVertexTransform(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Rmv2Geometry;
            var vert = geo.GetVertexExtented(vertexId);
            var m = GetAnimationVertex(frame, vertexId);
            return m;
            Matrix finalTransfrom = Matrix.CreateTranslation(new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z)) * m;
            return finalTransfrom;
        }


        Matrix GetAnimationVertex(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Rmv2Geometry;
            var vert = geo.GetVertexExtented(vertexId);

            var transformSum = Matrix.Identity;
            if (geo.WeightCount == 4)
            {
                int b0 = (int)vert.BlendIndices.X;
                int b1 = (int)vert.BlendIndices.Y;
                int b2 = (int)vert.BlendIndices.Z;
                int b3 = (int)vert.BlendIndices.W;

                float w1 = vert.BlendWeights.X;
                float w2 = vert.BlendWeights.Y;
                float w3 = vert.BlendWeights.Z;
                float w4 = vert.BlendWeights.W;

                Matrix m1 = frame.BoneTransforms[b0].WorldTransform;
                Matrix m2 = frame.BoneTransforms[b1].WorldTransform;
                Matrix m3 = frame.BoneTransforms[b2].WorldTransform;
                Matrix m4 = frame.BoneTransforms[b3].WorldTransform;
                transformSum.M11 = (m1.M11 * w1) + (m2.M11 * w2) + (m3.M11 * w3) + (m4.M11 * w4);
                transformSum.M12 = (m1.M12 * w1) + (m2.M12 * w2) + (m3.M12 * w3) + (m4.M12 * w4);
                transformSum.M13 = (m1.M13 * w1) + (m2.M13 * w2) + (m3.M13 * w3) + (m4.M13 * w4);
                transformSum.M21 = (m1.M21 * w1) + (m2.M21 * w2) + (m3.M21 * w3) + (m4.M21 * w4);
                transformSum.M22 = (m1.M22 * w1) + (m2.M22 * w2) + (m3.M22 * w3) + (m4.M22 * w4);
                transformSum.M23 = (m1.M23 * w1) + (m2.M23 * w2) + (m3.M23 * w3) + (m4.M23 * w4);
                transformSum.M31 = (m1.M31 * w1) + (m2.M31 * w2) + (m3.M31 * w3) + (m4.M31 * w4);
                transformSum.M32 = (m1.M32 * w1) + (m2.M32 * w2) + (m3.M32 * w3) + (m4.M32 * w4);
                transformSum.M33 = (m1.M33 * w1) + (m2.M33 * w2) + (m3.M33 * w3) + (m4.M33 * w4);
                transformSum.M41 = (m1.M41 * w1) + (m2.M41 * w2) + (m3.M41 * w3) + (m4.M41 * w4);
                transformSum.M42 = (m1.M42 * w1) + (m2.M42 * w2) + (m3.M42 * w3) + (m4.M42 * w4);
                transformSum.M43 = (m1.M43 * w1) + (m2.M43 * w2) + (m3.M43 * w3) + (m4.M43 * w4);
            }

            if (geo.WeightCount == 2)
            {
                int b0 = (int)vert.BlendIndices.X;
                int b1 = (int)vert.BlendIndices.Y;
                int b2 = (int)vert.BlendIndices.Z;
                int b3 = (int)vert.BlendIndices.W;

                float w1 = vert.BlendWeights.X;
                float w2 = vert.BlendWeights.Y;
                float w3 = vert.BlendWeights.Z;
                float w4 = vert.BlendWeights.W;

                Matrix m1 = frame.BoneTransforms[b0].WorldTransform;
                Matrix m2 = frame.BoneTransforms[b1].WorldTransform;
                Matrix m3 = frame.BoneTransforms[b2].WorldTransform;
                Matrix m4 = frame.BoneTransforms[b3].WorldTransform;

                transformSum.M11 = (m1.M11 * w1);
                transformSum.M12 = (m1.M12 * w1);
                transformSum.M13 = (m1.M13 * w1);
                transformSum.M21 = (m1.M21 * w1);
                transformSum.M22 = (m1.M22 * w1);
                transformSum.M23 = (m1.M23 * w1);
                transformSum.M31 = (m1.M31 * w1);
                transformSum.M32 = (m1.M32 * w1);
                transformSum.M33 = (m1.M33 * w1);
                transformSum.M41 = (m1.M41 * w1);
                transformSum.M42 = (m1.M42 * w1);
                transformSum.M43 = (m1.M43 * w1);

            }
            return transformSum * _worldTransform;
        }


    }
}
