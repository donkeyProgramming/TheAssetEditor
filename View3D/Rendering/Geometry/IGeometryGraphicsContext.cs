using Microsoft.Xna.Framework.Graphics;

namespace View3D.Rendering.Geometry
{
    public interface IGeometryGraphicsContext
    {
        IndexBuffer IndexBuffer { get; }
        VertexBuffer VertexBuffer { get; }

        IGeometryGraphicsContext Clone();
        void RebuildIndexBuffer(ushort[] indexList);
        void Dispose();
        void RebuildVertexBuffer<VertexType>(VertexType[] vertArray, VertexDeclaration vertexDeclaration) where VertexType : struct, IVertexType;
    }

    public class GeometryGraphicsContext : IGeometryGraphicsContext
    {
        GraphicsDevice Device;
        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }

        public GeometryGraphicsContext(GraphicsDevice device)
        {
            Device = device;
        }

        public void RebuildIndexBuffer(ushort[] indexList)
        {
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            if (indexList.Length != 0)
            {
                IndexBuffer = new IndexBuffer(Device, typeof(ushort), indexList.Length, BufferUsage.None);
                IndexBuffer.SetData(indexList);
            }
        }

        public virtual void RebuildVertexBuffer<VertexType>(VertexType[] vertArray, VertexDeclaration vertexDeclaration) where VertexType : struct, IVertexType
        {
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }

            if (vertArray.Length != 0)
            {
                VertexBuffer = new VertexBuffer(Device, vertexDeclaration, vertArray.Length, BufferUsage.None);
                VertexBuffer.SetData(vertArray);
            }
        }


        public IGeometryGraphicsContext Clone()
        {
            return new GeometryGraphicsContext(Device);
        }

        public void Dispose()
        {
            if (IndexBuffer != null)
                IndexBuffer.Dispose();
            if (VertexBuffer != null)
                VertexBuffer.Dispose();
        }
    }

    public interface IGeometryGraphicsContextFactory
    {
        IGeometryGraphicsContext Create();
    }
    public class GeometryGraphicsContextFactory : IGeometryGraphicsContextFactory
    {
        GraphicsDevice Device;
        public GeometryGraphicsContextFactory(GraphicsDevice device)
        {
            Device = device;
        }

        public static GeometryGraphicsContextFactory CreateInstance(GraphicsDevice device)
        {
            return new GeometryGraphicsContextFactory(device);
        }

        public IGeometryGraphicsContext Create()
        {
            return new GeometryGraphicsContext(Device);
        }
    }
}