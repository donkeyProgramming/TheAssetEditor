using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Geometry
{
    public class PrimitiveConstructor
    {
        private readonly IGeometryGraphicsContextFactory _geometryFactory;

        public PrimitiveConstructor(IGeometryGraphicsContextFactory geometryFactory)
        {
            _geometryFactory = geometryFactory;
        }

        public MeshObject CreateBox(UiVertexFormat vertexFormat, string skeletonName, int resolution = 10, float size = 1f)
        {
            resolution = Math.Max(1, resolution);

            var mesh = CreateMesh(vertexFormat, skeletonName);
            var halfSize = size * 0.5f;
            var vertices = new List<VertexPositionNormalTextureCustom>();
            var indices = new List<ushort>();

            AddFace(vertices, indices, resolution, new Vector3(halfSize, -halfSize, halfSize), new Vector3(0, 0, -size), new Vector3(0, size, 0), Vector3.Right, vertexFormat);
            AddFace(vertices, indices, resolution, new Vector3(-halfSize, -halfSize, -halfSize), new Vector3(0, 0, size), new Vector3(0, size, 0), Vector3.Left, vertexFormat);
            AddFace(vertices, indices, resolution, new Vector3(-halfSize, halfSize, halfSize), new Vector3(size, 0, 0), new Vector3(0, 0, -size), Vector3.Up, vertexFormat);
            AddFace(vertices, indices, resolution, new Vector3(-halfSize, -halfSize, -halfSize), new Vector3(size, 0, 0), new Vector3(0, 0, size), Vector3.Down, vertexFormat);
            AddFace(vertices, indices, resolution, new Vector3(-halfSize, -halfSize, halfSize), new Vector3(size, 0, 0), new Vector3(0, size, 0), Vector3.Forward, vertexFormat);
            AddFace(vertices, indices, resolution, new Vector3(halfSize, -halfSize, -halfSize), new Vector3(-size, 0, 0), new Vector3(0, size, 0), Vector3.Backward, vertexFormat);

            AssignGeometry(mesh, vertices, indices);
            return mesh;
        }

        public MeshObject CreatePlane(UiVertexFormat vertexFormat, string skeletonName, int resolution = 10, float size = 1f)
        {
            resolution = Math.Max(1, resolution);

            var mesh = CreateMesh(vertexFormat, skeletonName);
            var halfSize = size * 0.5f;
            var vertices = new List<VertexPositionNormalTextureCustom>();
            var indices = new List<ushort>();

            AddFace(vertices, indices, resolution, new Vector3(-halfSize, 0, -halfSize), new Vector3(size, 0, 0), new Vector3(0, 0, size), Vector3.Up, vertexFormat);

            AssignGeometry(mesh, vertices, indices);
            return mesh;
        }

        public MeshObject CreateSphere(UiVertexFormat vertexFormat, string skeletonName, int resolution = 10, float radius = 0.5f)
        {
            resolution = Math.Max(3, resolution);

            var mesh = CreateMesh(vertexFormat, skeletonName);
            var stacks = resolution;
            var slices = resolution * 2;
            var vertices = new List<VertexPositionNormalTextureCustom>();
            var indices = new List<ushort>();

            for (var stack = 0; stack <= stacks; stack++)
            {
                var v = stack / (float)stacks;
                var phi = MathF.PI * v;

                for (var slice = 0; slice <= slices; slice++)
                {
                    var u = slice / (float)slices;
                    var theta = MathF.PI * 2f * u;

                    var normal = new Vector3(
                        MathF.Sin(phi) * MathF.Cos(theta),
                        MathF.Cos(phi),
                        MathF.Sin(phi) * MathF.Sin(theta));

                    var tangent = new Vector3(
                        -MathF.Sin(theta),
                        0,
                        MathF.Cos(theta));

                    if (tangent.LengthSquared() < 0.0001f)
                        tangent = Vector3.Right;

                    var binormal = Vector3.Cross(normal, tangent);
                    if (binormal.LengthSquared() < 0.0001f)
                        binormal = Vector3.Up;

                    var position = normal * radius;
                    vertices.Add(CreateVertex(position, normal, tangent, binormal, new Vector2(u, v), vertexFormat));
                }
            }

            var stride = slices + 1;
            for (var stack = 0; stack < stacks; stack++)
            {
                for (var slice = 0; slice < slices; slice++)
                {
                    var topLeft = (ushort)((stack * stride) + slice);
                    var topRight = (ushort)(topLeft + 1);
                    var bottomLeft = (ushort)(topLeft + stride);
                    var bottomRight = (ushort)(bottomLeft + 1);

                    indices.Add(topLeft);
                    indices.Add(bottomRight);
                    indices.Add(topRight);

                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            AssignGeometry(mesh, vertices, indices);
            return mesh;
        }

        private MeshObject CreateMesh(UiVertexFormat vertexFormat, string skeletonName)
        {
            var mesh = new MeshObject(_geometryFactory.Create(), skeletonName);
            mesh.ChangeVertexType(vertexFormat, false);
            return mesh;
        }

        private static void AssignGeometry(MeshObject mesh, List<VertexPositionNormalTextureCustom> vertices, List<ushort> indices)
        {
            mesh.VertexArray = vertices.ToArray();
            mesh.IndexArray = indices.ToArray();
            mesh.RebuildVertexBuffer();
            mesh.RebuildIndexBuffer();
        }

        private static VertexPositionNormalTextureCustom CreateVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector3 binormal, Vector2 uv, UiVertexFormat vertexFormat)
        {
            var vertex = new VertexPositionNormalTextureCustom
            {
                Position = new Vector4(position, 1),
                Normal = Vector3.Normalize(normal),
                Tangent = Vector3.Normalize(tangent),
                BiNormal = Vector3.Normalize(binormal),
                TextureCoordinate = uv,
                TextureCoordinate1 = uv
            };

            if (vertexFormat == UiVertexFormat.Static)
            {
                vertex.BlendIndices = Vector4.Zero;
                vertex.BlendWeights = Vector4.Zero;
            }
            else
            {
                vertex.BlendIndices = Vector4.Zero;
                vertex.BlendWeights = new Vector4(1, 0, 0, 0);
            }

            return vertex;
        }

        private static void AddFace(List<VertexPositionNormalTextureCustom> vertices, List<ushort> indices, int resolution, Vector3 origin, Vector3 uAxis, Vector3 vAxis, Vector3 normal, UiVertexFormat vertexFormat)
        {
            var firstVertexIndex = vertices.Count;
            var tangent = Vector3.Normalize(uAxis);
            var binormal = Vector3.Normalize(vAxis);

            for (var y = 0; y <= resolution; y++)
            {
                var v = y / (float)resolution;
                for (var x = 0; x <= resolution; x++)
                {
                    var u = x / (float)resolution;
                    var position = origin + (uAxis * u) + (vAxis * v);
                    vertices.Add(CreateVertex(position, normal, tangent, binormal, new Vector2(u, v), vertexFormat));
                }
            }

            var useForwardWinding = Vector3.Dot(Vector3.Cross(tangent, binormal), normal) > 0;
            var stride = resolution + 1;
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var topLeft = (ushort)(firstVertexIndex + (y * stride) + x);
                    var topRight = (ushort)(topLeft + 1);
                    var bottomLeft = (ushort)(topLeft + stride);
                    var bottomRight = (ushort)(bottomLeft + 1);

                    if (useForwardWinding)
                    {
                        indices.Add(topLeft);
                        indices.Add(topRight);
                        indices.Add(bottomRight);

                        indices.Add(topLeft);
                        indices.Add(bottomRight);
                        indices.Add(bottomLeft);
                    }
                    else
                    {
                        indices.Add(topLeft);
                        indices.Add(bottomRight);
                        indices.Add(topRight);

                        indices.Add(topLeft);
                        indices.Add(bottomLeft);
                        indices.Add(bottomRight);
                    }
                }
            }
        }
    }
}