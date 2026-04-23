using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;

namespace GameWorld.Core.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPointInstanceData : IVertexType
    {
        public Vector3 InstancePosition;
        public float InstanceScale;
        public Vector3 InstanceColor;
        public float InstanceWeight;

        public static readonly VertexDeclaration VertexDeclaration;
        static VertexPointInstanceData()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Single, VertexElementUsage.Normal, 1),
                new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
                new VertexElement(sizeof(float) * 7, VertexElementFormat.Single, VertexElementUsage.Normal, 3),
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }

    public class VertexInstanceMesh : IDisposable
    {
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;
        Effect _effect;
        VertexDeclaration _instanceVertexDeclaration;

        DynamicVertexBuffer _instanceBuffer;
        VertexBuffer _geometryBuffer;
        IndexBuffer _indexBuffer;

        VertexBufferBinding[] _bindings;
        VertexPointInstanceData[] _instanceData;

        readonly int _maxInstanceCount = 50000;
        int _currentInstanceCount;

        Vector3 _selectedColour = new(1.0f, 0.47f, 0.0f);
        Vector3 _deselectedColour = new(0.0f, 0.0f, 0.0f);

        public float VertexPixelSize { get; set; } = 5.5f;
        public float SelectedSizeBoost { get; set; } = 2.0f;
        public float SelectionThresholdMultiplier { get; set; } = 2.0f;

        public VertexInstanceMesh(IDeviceResolver deviceResolverComponent, IScopedResourceLibrary resourceLibrary, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _graphicsResourceCreator = graphicsResourceCreator;
            Initialize(deviceResolverComponent.Device, resourceLibrary);
        }

        void Initialize(GraphicsDevice device, IScopedResourceLibrary resourceLib)
        {
            _effect = resourceLib.GetStaticEffect(ShaderTypes.VertexPoint);

            _instanceVertexDeclaration = VertexPointInstanceData.VertexDeclaration;
            GenerateGeometry(device);
            _instanceBuffer = _graphicsResourceCreator.CreateDynamicVertexBuffer(_instanceVertexDeclaration, _maxInstanceCount, BufferUsage.WriteOnly);
            _instanceData = new VertexPointInstanceData[_maxInstanceCount];

            _bindings = new VertexBufferBinding[2];
            _bindings[0] = new VertexBufferBinding(_geometryBuffer);
            _bindings[1] = new VertexBufferBinding(_instanceBuffer, 0, 1);
        }

        void GenerateGeometry(GraphicsDevice device)
        {
            var vertices = new VertexPositionTexture[4];
            vertices[0] = new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1));
            vertices[1] = new VertexPositionTexture(new Vector3(0.5f, -0.5f, 0), new Vector2(1, 1));
            vertices[2] = new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 0));
            vertices[3] = new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 0));

            _geometryBuffer = _graphicsResourceCreator.CreateVertexBuffer(VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            _geometryBuffer.SetData(vertices);

            var indices = new int[6];
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 1; indices[4] = 3; indices[5] = 2;

            _indexBuffer = _graphicsResourceCreator.CreateIndexBuffer(typeof(int), 6, BufferUsage.WriteOnly);
            _indexBuffer.SetData(indices);
        }

        public void Update(MeshObject geo, Matrix modelMatrix, Vector3 cameraPos,
            float cameraFov, float viewportHeight, VertexSelectionState selectedVertexes)
        {
            _currentInstanceCount = Math.Min(geo.VertexCount(), _maxInstanceCount);

            float fovScale = 2.0f * MathF.Tan(cameraFov / 2.0f) / viewportHeight;

            for (var i = 0; i < _currentInstanceCount && i < _maxInstanceCount; i++)
            {
                var vertPos = Vector3.Transform(geo.GetVertexById(i), modelMatrix);
                var distance = (cameraPos - vertPos).Length();

                var weight = selectedVertexes.VertexWeights[i];
                var color = Vector3.Lerp(_deselectedColour, _selectedColour, weight);

                var effectivePixelSize = VertexPixelSize + weight * SelectedSizeBoost;
                var worldScale = effectivePixelSize * distance * fovScale;

                _instanceData[i].InstancePosition = vertPos;
                _instanceData[i].InstanceScale = worldScale;
                _instanceData[i].InstanceColor = color;
                _instanceData[i].InstanceWeight = weight;
            }

            _instanceBuffer.SetData(_instanceData, 0, Math.Min(_currentInstanceCount, _maxInstanceCount), SetDataOptions.None);
        }

        public void Draw(Matrix view, Matrix projection, Vector3 cameraPos, GraphicsDevice device)
        {
            _effect.CurrentTechnique = _effect.Techniques["VertexPoint"];
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["ViewProjection"].SetValue(view * projection);
            _effect.Parameters["CameraPosition"].SetValue(cameraPos);

            device.BlendState = BlendState.AlphaBlend;

            device.Indices = _indexBuffer;
            _effect.CurrentTechnique.Passes[0].Apply();

            device.SetVertexBuffers(_bindings);
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2, _currentInstanceCount);

            device.BlendState = BlendState.Opaque;
        }

        public void Dispose()
        {
            _instanceVertexDeclaration = null;
            _instanceBuffer = _graphicsResourceCreator.DisposeTracked(_instanceBuffer);
            _geometryBuffer = _graphicsResourceCreator.DisposeTracked(_geometryBuffer);
            _indexBuffer = _graphicsResourceCreator.DisposeTracked(_indexBuffer);
        }
    }
}
