using GameWorld.Core.Utility;
using GameWorld.Core.Services;
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
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;
        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }

        public GraphicsCardGeometry(GraphicsDevice device, IGraphicsResourceCreator graphicsResourceCreator)
        {
            Device = device;
            _graphicsResourceCreator = graphicsResourceCreator;
        }

        public void RebuildIndexBuffer(ushort[] indexList)
        {
            IndexBuffer = _graphicsResourceCreator.DisposeTracked(IndexBuffer);

            if (indexList.Length != 0)
            {
                IndexBuffer = _graphicsResourceCreator.CreateIndexBuffer(typeof(ushort), indexList.Length, BufferUsage.None);
                IndexBuffer.SetData(indexList);
            }
        }

        public virtual void RebuildVertexBuffer(VertexPositionNormalTextureCustom[] vertArray, VertexDeclaration vertexDeclaration)
        {
            VertexBuffer = _graphicsResourceCreator.DisposeTracked(VertexBuffer);

            if (vertArray.Length != 0)
            {
                VertexBuffer = _graphicsResourceCreator.CreateVertexBuffer(vertexDeclaration, vertArray.Length, BufferUsage.None);
                VertexBuffer.SetData(vertArray);
            }
        }

        public IGraphicsCardGeometry Clone()
        {
            return new GraphicsCardGeometry(Device, _graphicsResourceCreator);
        }

        public void Dispose()
        {
            IndexBuffer = _graphicsResourceCreator.DisposeTracked(IndexBuffer);
            VertexBuffer = _graphicsResourceCreator.DisposeTracked(VertexBuffer);
        }
    }

    public interface IGeometryGraphicsContextFactory
    {
        IGraphicsCardGeometry Create();
    }
    public class GeometryGraphicsContextFactory : IGeometryGraphicsContextFactory
    {
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        public GeometryGraphicsContextFactory(IDeviceResolver deviceResolverComponent, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _deviceResolverComponent = deviceResolverComponent;
            _graphicsResourceCreator = graphicsResourceCreator;
        }

        public IGraphicsCardGeometry Create()
        {
            return new GraphicsCardGeometry(_deviceResolverComponent.Device, _graphicsResourceCreator);
        }
    }
}
