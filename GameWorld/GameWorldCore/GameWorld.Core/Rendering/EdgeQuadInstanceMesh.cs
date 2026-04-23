using System;
using System.Runtime.InteropServices;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EdgeQuadInstanceData : IVertexType
    {
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 C0;
        public Vector3 C1;
        public float Width;

        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Position, 2),
            new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Color, 1),
            new VertexElement(36, VertexElementFormat.Vector3, VertexElementUsage.Color, 2),
            new VertexElement(48, VertexElementFormat.Single, VertexElementUsage.BlendWeight, 0));

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }

    public struct EdgeData
    {
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 C0;
        public Vector3 C1;
        public float Width;
    }

    public class EdgeQuadInstanceMesh : IDisposable
    {
        private const int MaxInstances = 50000;
        private readonly GraphicsDevice _device;
        private readonly Effect _effect;
        private VertexBuffer _quadVb;
        private IndexBuffer _quadIb;
        private DynamicVertexBuffer _instanceVb;
        private readonly EdgeQuadInstanceData[] _instanceData = new EdgeQuadInstanceData[MaxInstances];
        private int _instanceCount;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        public EdgeQuadInstanceMesh(IDeviceResolver deviceResolver, IScopedResourceLibrary resourceLib, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _device = deviceResolver.Device;
            _graphicsResourceCreator = graphicsResourceCreator;
            _effect = resourceLib.GetStaticEffect(ShaderTypes.EdgeQuad);
            BuildQuadGeometry();
        }

        void BuildQuadGeometry()
        {
            var verts = new VertexPosition[]
            {
                new(new Vector3(0, -0.5f, 0)),
                new(new Vector3(0, 0.5f, 0)),
                new(new Vector3(1, 0.5f, 0)),
                new(new Vector3(1, -0.5f, 0)),
            };

            _quadVb = _graphicsResourceCreator.CreateVertexBuffer(VertexPosition.VertexDeclaration, 4, BufferUsage.WriteOnly);
            _quadVb.SetData(verts);

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };
            _quadIb = _graphicsResourceCreator.CreateIndexBuffer(typeof(short), 6, BufferUsage.WriteOnly);
            _quadIb.SetData(indices);

            _instanceVb = _graphicsResourceCreator.CreateDynamicVertexBuffer(EdgeQuadInstanceData.VertexDeclaration, MaxInstances, BufferUsage.WriteOnly);
        }

        public void Update(EdgeData[] edges, int count, CommonShaderParameters shaderParams)
        {
            _instanceCount = Math.Min(count, MaxInstances);
            for (var i = 0; i < _instanceCount; i++)
            {
                _instanceData[i] = new EdgeQuadInstanceData
                {
                    P0 = edges[i].P0,
                    P1 = edges[i].P1,
                    C0 = edges[i].C0,
                    C1 = edges[i].C1,
                    Width = edges[i].Width
                };
            }

            if (_instanceCount > 0)
                _instanceVb.SetData(_instanceData, 0, _instanceCount);
        }

        public void Draw(CommonShaderParameters shaderParams, GraphicsDevice device)
        {
            if (_instanceCount == 0) return;

            _effect.Parameters["View"]?.SetValue(shaderParams.View);
            _effect.Parameters["Projection"]?.SetValue(shaderParams.Projection);
            _effect.Parameters["ViewportWidth"]?.SetValue(shaderParams.ViewportWidth);
            _effect.Parameters["ViewportHeight"]?.SetValue(shaderParams.ViewportHeight);

            device.BlendState = BlendState.AlphaBlend;

            _device.SetVertexBuffers(
                new VertexBufferBinding(_quadVb, 0, 0),
                new VertexBufferBinding(_instanceVb, 0, 1));
            _device.Indices = _quadIb;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, _instanceCount);
            }

            device.BlendState = BlendState.Opaque;
        }

        public void Dispose()
        {
            _quadVb = _graphicsResourceCreator.DisposeTracked(_quadVb);
            _quadIb = _graphicsResourceCreator.DisposeTracked(_quadIb);
            _instanceVb = _graphicsResourceCreator.DisposeTracked(_instanceVb);
        }
    }
}
