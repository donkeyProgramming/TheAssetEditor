using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering
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
        public Vector2 TextureCoordinate1;

        public readonly static VertexDeclaration VertexDeclaration
            = new(
                    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                    new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(36, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
                    new VertexElement(48, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
                    new VertexElement(60, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
                    new VertexElement(76, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0),
                    new VertexElement(92, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
                );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }


        public float[] GetBoneWeights()
        {
            return [BlendWeights.X, BlendWeights.Y, BlendWeights.Z, BlendWeights.W];
        }

        public int[] GetBoneIndexs()
        {
            return [(int)BlendIndices.X, (int)BlendIndices.Y, (int)BlendIndices.Z, (int)BlendIndices.W];
        }

        public Vector3 Position3()
        {
            return new Vector3(Position.X, Position.Y, Position.Z);
        }

    }
}
