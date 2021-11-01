using Microsoft.Xna.Framework.Graphics;

namespace View3D.Rendering.Geometry
{
    public interface IGraphicsCardGeometry
    {
        IndexBuffer IndexBuffer { get; }
        VertexBuffer VertexBuffer { get; }


        void RebuildIndexBuffer(ushort[] indexList);
        void RebuildVertexBuffer(VertexPositionNormalTextureCustom[] vertArray, VertexDeclaration vertexDeclaration);

        IGraphicsCardGeometry Clone();
        void Dispose();
    }

    public class GraphicsCardGeometry : IGraphicsCardGeometry
    {
        GraphicsDevice Device;
        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }

        public GraphicsCardGeometry(GraphicsDevice device)
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

        public virtual void RebuildVertexBuffer(VertexPositionNormalTextureCustom[] vertArray, VertexDeclaration vertexDeclaration)
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


        public IGraphicsCardGeometry Clone()
        {
            return new GraphicsCardGeometry(Device);
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
        IGraphicsCardGeometry Create();
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

        public IGraphicsCardGeometry Create()
        {
            return new GraphicsCardGeometry(Device);
        }
    }
}