using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Services;
using GameWorld.Core.Test.TestUtility;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Test.Services
{
    internal class MeshBuilderServiceTests
    {
        [TestCase]
        public void CreateRmvMeshFromGeometry_InvalidVertexWeights_Throws()
        {
            // Arrange
            var meshBuilderService = new MeshBuilderService(new TestGeometryGraphicsContextFactory());
            var mesh = CreateWeightedTriangleMesh();
            mesh.VertexArray[0].BlendWeights = new Vector4(0.25f, 0.25f, 0, 0);

            // Act
            var act = () => meshBuilderService.CreateRmvMeshFromGeometry(mesh, 2, 7, "test_mesh");

            // Assert
            Assert.That(act, Throws.TypeOf<InvalidOperationException>().With.Message.Contains("LodIndex:2"));
            Assert.That(act, Throws.TypeOf<InvalidOperationException>().With.Message.Contains("MeshId:7"));
            Assert.That(act, Throws.TypeOf<InvalidOperationException>().With.Message.Contains("MeshName:'test_mesh'"));
        }

        static MeshObject CreateWeightedTriangleMesh()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            mesh.ChangeVertexType(UiVertexFormat.Weighted, false);

            for (var i = 0; i < mesh.VertexArray.Length; i++)
            {
                mesh.VertexArray[i].Normal = Vector3.Up;
                mesh.VertexArray[i].BiNormal = Vector3.Right;
                mesh.VertexArray[i].Tangent = Vector3.Forward;
                mesh.VertexArray[i].BlendIndices = new Vector4(0, 1, 0, 0);
                mesh.VertexArray[i].BlendWeights = new Vector4(0.5f, 0.5f, 0, 0);
            }

            return mesh;
        }
    }
}