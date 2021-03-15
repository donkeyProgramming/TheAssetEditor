using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering.Geometry;

namespace FileTypesTests.Util
{
    class TestGeometryGraphicsContextFactory : IGeometryGraphicsContextFactory
    {
        public IGeometryGraphicsContext Create()
        {
            return new TestGeometryGraphicsContext(); ;
        }
    }


    class TestGeometryGraphicsContext : IGeometryGraphicsContext
    {
        public IndexBuffer IndexBuffer => throw new NotImplementedException();

        public VertexBuffer VertexBuffer => throw new NotImplementedException();

        public IGeometryGraphicsContext Clone()
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
    }
}
