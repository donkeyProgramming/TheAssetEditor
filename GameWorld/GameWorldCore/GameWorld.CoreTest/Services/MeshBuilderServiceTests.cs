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
        public void NormalizeBoneWeights_Weighted2_NormalizesAndClearsZW()
        {
            // Arrange
            var mesh = CreateWeightedTriangleMesh();
            mesh.VertexArray[0].BlendWeights = new Vector4(0.1f, 0.2f, 9f, 9f);

            // Act
            MeshBuilderService.NormalizeBoneWeights(mesh);

            // Assert
            var w = mesh.VertexArray[0].BlendWeights;
            Assert.That(w.X + w.Y, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(w.Z, Is.EqualTo(0f));
            Assert.That(w.W, Is.EqualTo(0f));
        }

        [TestCase]
        public void NormalizeBoneWeights_Cinematic_NormalizesAll4Weights()
        {
            // Arrange
            var mesh = CreateCinematicTriangleMesh();
            mesh.VertexArray[0].BlendWeights = new Vector4(1f, 2f, 3f, 4f);

            // Act
            MeshBuilderService.NormalizeBoneWeights(mesh);

            // Assert
            var w = mesh.VertexArray[0].BlendWeights;
            Assert.That(w.X + w.Y + w.Z + w.W, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(w.X, Is.EqualTo(0.1f).Within(0.00001f));
            Assert.That(w.Y, Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(w.Z, Is.EqualTo(0.3f).Within(0.00001f));
            Assert.That(w.W, Is.EqualTo(0.4f).Within(0.00001f));
        }

        [TestCase]
        public void NormalizeBoneWeights_ZeroTotal_FallsBackTo1000()
        {
            // Arrange
            var mesh = CreateWeightedTriangleMesh();
            mesh.VertexArray[0].BlendWeights = Vector4.Zero;

            // Act
            MeshBuilderService.NormalizeBoneWeights(mesh);

            // Assert
            var w = mesh.VertexArray[0].BlendWeights;
            Assert.That(w.X, Is.EqualTo(1f));
            Assert.That(w.Y, Is.EqualTo(0f));
            Assert.That(w.Z, Is.EqualTo(0f));
            Assert.That(w.W, Is.EqualTo(0f));
        }

        [TestCase]
        public void NormalizeBoneWeights_StaticMesh_DoesNothing()
        {
            // Arrange
            var mesh = MeshTestHelper.CreateTriangleMesh();
            mesh.ChangeVertexType(UiVertexFormat.Static, false);
            mesh.VertexArray[0].BlendWeights = new Vector4(0.2f, 0.3f, 0.4f, 0.1f);

            // Act
            MeshBuilderService.NormalizeBoneWeights(mesh);

            // Assert
            var w = mesh.VertexArray[0].BlendWeights;
            Assert.That(w.X, Is.EqualTo(0.2f));
            Assert.That(w.Y, Is.EqualTo(0.3f));
            Assert.That(w.Z, Is.EqualTo(0.4f));
            Assert.That(w.W, Is.EqualTo(0.1f));
        }

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

        static MeshObject CreateCinematicTriangleMesh()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            mesh.ChangeVertexType(UiVertexFormat.Cinematic, false);

            for (var i = 0; i < mesh.VertexArray.Length; i++)
            {
                mesh.VertexArray[i].Normal = Vector3.Up;
                mesh.VertexArray[i].BiNormal = Vector3.Right;
                mesh.VertexArray[i].Tangent = Vector3.Forward;
                mesh.VertexArray[i].BlendIndices = new Vector4(0, 1, 2, 3);
                mesh.VertexArray[i].BlendWeights = new Vector4(0.25f, 0.25f, 0.25f, 0.25f);
            }

            return mesh;
        }
    }
}