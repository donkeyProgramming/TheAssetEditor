using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Test.TestUtility
{
    public class TestGeometryGraphicsContextFactory : IGeometryGraphicsContextFactory
    {
        public IGraphicsCardGeometry Create() => new TestGraphicsCardGeometry();
    }

    public class TestGraphicsCardGeometry : IGraphicsCardGeometry
    {
        public IndexBuffer IndexBuffer { get; }
        public VertexBuffer VertexBuffer { get; }

        public void RebuildIndexBuffer(ushort[] indexList)
        { }
        public void RebuildVertexBuffer(VertexPositionNormalTextureCustom[] vertArray, VertexDeclaration vertexDeclaration)
        { }

        public IGraphicsCardGeometry Clone() { return this; }
        public void Dispose()
        { }
    }
}
