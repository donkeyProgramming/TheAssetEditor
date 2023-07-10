using Microsoft.Xna.Framework.Graphics;
using System;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace FileTypesTests.Util
{
    class TestGeometryGraphicsContextFactory : IGeometryGraphicsContextFactory
    {
        public IGraphicsCardGeometry Create()
        {
            return new TestGeometryGraphicsContext(); ;
        }
    }


    class TestGeometryGraphicsContext : IGraphicsCardGeometry
    {
        public IndexBuffer IndexBuffer => throw new NotImplementedException();

        public VertexBuffer VertexBuffer => throw new NotImplementedException();

        public IGraphicsCardGeometry Clone()
        {
            return new TestGeometryGraphicsContext();
        }

        public void Dispose()
        {

        }

        public void RebuildIndexBuffer(ushort[] indexList)
        {
        }

        public void RebuildVertexBuffer<VertexType>(VertexType[] vertArray, VertexDeclaration vertexDeclaration) where VertexType : struct, IVertexType
        {

        }

        public void RebuildVertexBuffer(VertexPositionNormalTextureCustom[] vertArray, VertexDeclaration vertexDeclaration)
        {
            throw new NotImplementedException();
        }
    }
}
