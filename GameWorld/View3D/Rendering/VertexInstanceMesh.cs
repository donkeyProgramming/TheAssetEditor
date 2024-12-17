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
    public struct InstanceDataOrientation : IVertexType
    {
        public Vector3 instanceForward;
        public Vector3 instanceUp;
        public Vector3 instanceLeft;
        public Vector3 instancePosition;

        public static readonly VertexDeclaration VertexDeclaration;
        static InstanceDataOrientation()
        {
            var elements = new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1), // The usage index must match.
                    new VertexElement(sizeof(float) *3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
                    new VertexElement(sizeof(float) *6, VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
                    new VertexElement(sizeof(float) *9, VertexElementFormat.Vector3, VertexElementUsage.Normal, 3),
                    new VertexElement(sizeof(float) *12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 4),
                    //new VertexElement(48, VertexElementFormat.Single, VertexElementUsage.BlendWeight, 0)
                    //new VertexElement( offset in bytes, VertexElementFormat.Single, VertexElementUsage. option, shader element usage id number )
                };
            VertexDeclaration = new VertexDeclaration(elements);
        }
        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    struct VertexMeshInstanceInfo
    {
        public Vector3 World0 { get; set; }
        public Vector3 World1 { get; set; }
        public Vector3 World2 { get; set; }
        public Vector3 World3 { get; set; }
        public Vector3 Colour { get; set; }
    };

    public class VertexInstanceMesh : IDisposable
    {
        Effect _effect;
        VertexDeclaration _instanceVertexDeclaration;

        DynamicVertexBuffer _instanceBuffer;
        VertexBuffer _geometryBuffer;
        IndexBuffer _indexBuffer;

        VertexBufferBinding[] _bindings;
        VertexMeshInstanceInfo[] _instanceTransform;

        readonly int _maxInstanceCount = 50000;
        int _currentInstanceCount;

        Vector3 _selectedColour = new(1, 0, 0);
        Vector3 _deselectedColour = new (1, 1, 1);

        public VertexInstanceMesh(IDeviceResolver deviceResolverComponent, IScopedResourceLibrary resourceLibrary)
        {
            Initialize(deviceResolverComponent.Device, resourceLibrary);
        }

        void Initialize(GraphicsDevice device, IScopedResourceLibrary resourceLib)
        {
            _effect = resourceLib.GetStaticEffect(ShaderTypes.GeometryInstance);

            _instanceVertexDeclaration = InstanceDataOrientation.VertexDeclaration;
            GenerateGeometry(device);
            _instanceBuffer = new DynamicVertexBuffer(device, _instanceVertexDeclaration, _maxInstanceCount, BufferUsage.WriteOnly);
            _instanceTransform = new VertexMeshInstanceInfo[_maxInstanceCount];
            GenerateInstanceInformation(_maxInstanceCount);

            _bindings = new VertexBufferBinding[2];
            _bindings[0] = new VertexBufferBinding(_geometryBuffer);
            _bindings[1] = new VertexBufferBinding(_instanceBuffer, 0, 1);
        }

        void GenerateGeometry(GraphicsDevice device)
        {
            var vertices = new VertexPosition[24];
            vertices[0].Position = new Vector3(-1, 1, -1);
            vertices[1].Position = new Vector3(1, 1, -1);
            vertices[2].Position = new Vector3(-1, 1, 1);
            vertices[3].Position = new Vector3(1, 1, 1);

            vertices[4].Position = new Vector3(-1, -1, 1);
            vertices[5].Position = new Vector3(1, -1, 1);
            vertices[6].Position = new Vector3(-1, -1, -1);
            vertices[7].Position = new Vector3(1, -1, -1);

            vertices[8].Position = new Vector3(-1, 1, -1);
            vertices[9].Position = new Vector3(-1, 1, 1);
            vertices[10].Position = new Vector3(-1, -1, -1);
            vertices[11].Position = new Vector3(-1, -1, 1);

            vertices[12].Position = new Vector3(-1, 1, 1);
            vertices[13].Position = new Vector3(1, 1, 1);
            vertices[14].Position = new Vector3(-1, -1, 1);
            vertices[15].Position = new Vector3(1, -1, 1);

            vertices[16].Position = new Vector3(1, 1, 1);
            vertices[17].Position = new Vector3(1, 1, -1);
            vertices[18].Position = new Vector3(1, -1, 1);
            vertices[19].Position = new Vector3(1, -1, -1);

            vertices[20].Position = new Vector3(1, 1, -1);
            vertices[21].Position = new Vector3(-1, 1, -1);
            vertices[22].Position = new Vector3(1, -1, -1);
            vertices[23].Position = new Vector3(-1, -1, -1);

            _geometryBuffer = new VertexBuffer(device, VertexPosition.VertexDeclaration, 24, BufferUsage.WriteOnly);
            _geometryBuffer.SetData(vertices);

            var indices = new int[36];
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 1; indices[4] = 3; indices[5] = 2;

            indices[6] = 4; indices[7] = 5; indices[8] = 6;
            indices[9] = 5; indices[10] = 7; indices[11] = 6;

            indices[12] = 8; indices[13] = 9; indices[14] = 10;
            indices[15] = 9; indices[16] = 11; indices[17] = 10;

            indices[18] = 12; indices[19] = 13; indices[20] = 14;
            indices[21] = 13; indices[22] = 15; indices[23] = 14;

            indices[24] = 16; indices[25] = 17; indices[26] = 18;
            indices[27] = 17; indices[28] = 19; indices[29] = 18;

            indices[30] = 20; indices[31] = 21; indices[32] = 22;
            indices[33] = 21; indices[34] = 23; indices[35] = 22;

            _indexBuffer = new IndexBuffer(device, typeof(int), 36, BufferUsage.WriteOnly);
            _indexBuffer.SetData(indices);
        }

        public void Update(MeshObject geo, Matrix modelMatrix, Quaternion objectRotation, Vector3 cameraPos, VertexSelectionState selectedVertexes)
        {
            _currentInstanceCount = geo.VertexCount();
            for (var i = 0; i < _currentInstanceCount && i < _maxInstanceCount; i++)
            {
                var vertPos = Vector3.Transform(geo.GetVertexById(i), modelMatrix);
                var distance = (cameraPos - vertPos).Length();
                var distanceScale = distance * 1.5f;

                var world = Matrix.CreateScale(0.0025f * distanceScale) * Matrix.CreateFromQuaternion(objectRotation) * Matrix.CreateTranslation(vertPos);

                _instanceTransform[i].World0 = new Vector3(world[0, 0], world[0, 1], world[0, 2]);
                _instanceTransform[i].World1 = new Vector3(world[1, 0], world[1, 1], world[1, 2]);
                _instanceTransform[i].World2 = new Vector3(world[2, 0], world[2, 1], world[2, 2]);
                _instanceTransform[i].World3 = new Vector3(world[3, 0], world[3, 1], world[3, 2]);
                _instanceTransform[i].Colour = Vector3.Lerp(_deselectedColour, _selectedColour, selectedVertexes.VertexWeights[i]);

            }
            _instanceBuffer.SetData(_instanceTransform, 0, Math.Min(_currentInstanceCount, _maxInstanceCount), SetDataOptions.None);
        }

        private void GenerateInstanceInformation(int count)
        {
            var rnd = new Random();

            for (var i = 0; i < count; i++)
            {
                var world = Matrix.CreateScale((float)rnd.NextDouble() * 1) *
                    Matrix.CreateRotationZ((float)rnd.NextDouble()) *
                    Matrix.CreateTranslation((float)rnd.NextDouble() * 20, (float)rnd.NextDouble() * 20, (float)rnd.NextDouble() * 20);

                _instanceTransform[i].World0 = new Vector3(world[0, 0], world[0, 1], world[0, 2]);
                _instanceTransform[i].World1 = new Vector3(world[1, 0], world[1, 1], world[1, 2]);
                _instanceTransform[i].World2 = new Vector3(world[2, 0], world[2, 1], world[2, 2]);
                _instanceTransform[i].World3 = new Vector3(world[3, 0], world[3, 1], world[3, 2]);
            }
            _instanceBuffer.SetData(_instanceTransform);
        }

        public void Draw(Matrix view, Matrix projection, GraphicsDevice device, Vector3 colour)
        {
            _effect.CurrentTechnique = _effect.Techniques["Instancing"];
            _effect.Parameters["WVP"].SetValue(view * projection);
            _effect.Parameters["VertexColour"].SetValue(colour);

            device.Indices = _indexBuffer;
            _effect.CurrentTechnique.Passes[0].Apply();

            device.SetVertexBuffers(_bindings);
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 24, 0, 12, _currentInstanceCount);
        }

        public void Dispose()
        {
            _instanceVertexDeclaration.Dispose();
            _instanceBuffer.Dispose();
        }
    }
}
