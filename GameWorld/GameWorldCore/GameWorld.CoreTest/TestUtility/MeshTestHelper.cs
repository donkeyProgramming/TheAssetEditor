using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Moq;

namespace GameWorld.Core.Test.TestUtility
{
    public static class MeshTestHelper
    {
        public static MeshObject CreateTriangleMesh()
        {
            var contextFactory = new TestGeometryGraphicsContextFactory();
            var mesh = new MeshObject(contextFactory.Create(), "test_skeleton");
            mesh.VertexArray = new VertexPositionNormalTextureCustom[]
            {
                new() { Position = new Vector4(0, 0, 0, 1) },
                new() { Position = new Vector4(1, 0, 0, 1) },
                new() { Position = new Vector4(0, 1, 0, 1) },
            };
            mesh.IndexArray = new ushort[] { 0, 1, 2 };
            return mesh;
        }

        public static MeshObject CreateQuadMesh()
        {
            var contextFactory = new TestGeometryGraphicsContextFactory();
            var mesh = new MeshObject(contextFactory.Create(), "test_skeleton");
            mesh.VertexArray = new VertexPositionNormalTextureCustom[]
            {
                new() { Position = new Vector4(0, 0, 0, 1) },
                new() { Position = new Vector4(1, 0, 0, 1) },
                new() { Position = new Vector4(1, 1, 0, 1) },
                new() { Position = new Vector4(0, 1, 0, 1) },
            };
            mesh.IndexArray = new ushort[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }

        public static ISelectable CreateSelectable(MeshObject mesh)
        {
            var mock = new Mock<ISelectable>();
            mock.Setup(x => x.Geometry).Returns(mesh);
            mock.Setup(x => x.RenderMatrix).Returns(Matrix.Identity);
            mock.Setup(x => x.Name).Returns("TestMesh");
            return mock.Object;
        }
    }
}
