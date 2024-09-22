using GameWorld.Core.Utility;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Geometry
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
        private readonly GraphicsDevice Device;
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
        private readonly IDeviceResolver _deviceResolverComponent;

        public GeometryGraphicsContextFactory(IDeviceResolver deviceResolverComponent)
        {
            _deviceResolverComponent = deviceResolverComponent;
        }

        public IGraphicsCardGeometry Create()
        {
            return new GraphicsCardGeometry(_deviceResolverComponent.Device);
        }
    }






}
