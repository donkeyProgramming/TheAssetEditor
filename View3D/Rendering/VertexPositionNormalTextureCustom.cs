using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Rendering
{
    public struct VertexPositionNormalTextureCustom : IVertexType
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector3 Tangent;
        public Vector3 BiNormal;
        public Vector4 BlendWeights;
        public Vector4 BlendIndices;

        public readonly static VertexDeclaration VertexDeclaration
            = new VertexDeclaration(
                    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                    new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(36, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
                    new VertexElement(48, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
                    new VertexElement(60, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
                    new VertexElement(76, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0)
                );

        public VertexPositionNormalTextureCustom(Vector3 pos, Vector3 normal, Vector2 tex, Vector3 tangent = new Vector3(), Vector3 biNormal = new Vector3())
        {
            Position = new Vector4(pos, 1);
            Normal = normal;
            TextureCoordinate = tex;
            Tangent = tangent;
            BiNormal = biNormal;
            BlendWeights = Vector4.One;
            BlendIndices = Vector4.Zero; //; new short[4] { 0, 0, 0, 0 };
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }


        public float[] GetBoneWeights()
        {
            return new float[] { BlendWeights.X, BlendWeights.Y, BlendWeights.Z, BlendWeights.W };
        }

        public int[] GetBoneIndexs()
        {
            return new int[] { (int)BlendIndices.X, (int)BlendIndices.Y, (int)BlendIndices.Z, (int)BlendIndices.W };
        }

    }
}
