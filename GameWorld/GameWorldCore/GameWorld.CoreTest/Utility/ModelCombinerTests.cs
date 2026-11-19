using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Test.TestUtility;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Moq;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Test.GameWorld.Core.WsMaterialTemplate;

namespace GameWorld.Core.Test.Utility
{
    [TestFixture]
    public class ModelCombinerTests
    {
        [Test]
        public void Merge_UpToTheVertexLimit_IndexesEveryVertex()
        {
            var mesh = CreateMesh(RmvMesh.MaxVertexCount / 2);
            mesh.Merge([CreateMesh(RmvMesh.MaxVertexCount / 2)]);

            Assert.That(mesh.VertexCount(), Is.EqualTo(RmvMesh.MaxVertexCount));
            Assert.That(mesh.IndexArray.Max(), Is.EqualTo(RmvMesh.MaxVertexCount - 1));
        }

        [Test]
        public void Merge_PastTheVertexLimit_ThrowsAndLeavesTheMeshAlone()
        {
            var mesh = CreateMesh(40_000);

            Assert.Throws<InvalidOperationException>(() => mesh.Merge([CreateMesh(40_000)]));
            Assert.That(mesh.VertexCount(), Is.EqualTo(40_000));
        }

        [Test]
        public void CombineMeshes_StartsANewMeshWhenTheNextWouldPassTheVertexLimit()
        {
            var meshes = new List<Rmv2MeshNode> { CreateNode("a", 30_000), CreateNode("b", 30_000), CreateNode("c", 30_000) };

            var combined = ModelCombiner.CombineMeshes(meshes);

            Assert.That(combined.Select(x => x.Geometry.VertexCount()), Is.EqualTo(new[] { 60_000, 30_000 }));
            Assert.That(combined[0].Geometry.IndexArray.Max(), Is.EqualTo(59_999));
        }

        [Test]
        public void CombineMeshes_IndexCountIsNotLimitedTo16Bits()
        {
            var meshes = new List<Rmv2MeshNode> { CreateNode("a", 100, indexCount: 63_000), CreateNode("b", 100, indexCount: 63_000) };

            Assert.That(ModelCombiner.GetVertexLimitErrors(meshes).HasData, Is.False);

            var combined = ModelCombiner.CombineMeshes(meshes);

            Assert.That(combined, Has.Count.EqualTo(1));
            Assert.That(combined[0].Geometry.GetIndexCount(), Is.EqualTo(126_000));
            Assert.That(combined[0].Geometry.IndexArray.Max(), Is.EqualTo(199));
        }

        [Test]
        public void GetVertexLimitErrors_ReportsOnlyCombinableMeshesOverTheLimit()
        {
            var meshes = new List<Rmv2MeshNode>
            {
                CreateNode("a", 40_000),
                CreateNode("b", 40_000),
                CreateNode("other_material", 40_000, CapabilityMaterialsEnum.MetalRoughPbr_Default),
            };

            var errors = ModelCombiner.GetVertexLimitErrors(meshes);

            Assert.That(errors.Errors, Has.Count.EqualTo(1));
            Assert.That(errors.Errors[0].Description, Does.Contain("a, b").And.Contain("80000"));
        }

        [Test]
        public void GetVertexLimitErrors_IgnoresAMeshOverTheLimitWithNothingToCombineWith()
        {
            var meshes = new List<Rmv2MeshNode>
            {
                CreateNode("too_big", 70_000, CapabilityMaterialsEnum.MetalRoughPbr_Default),
                CreateNode("a", 100),
                CreateNode("b", 100),
            };

            Assert.That(ModelCombiner.GetVertexLimitErrors(meshes).HasData, Is.False);
        }

        static MeshObject CreateMesh(int vertexCount, int? indexCount = null)
        {
            var mesh = new MeshObject(new TestGeometryGraphicsContextFactory().Create(), "test_skeleton");
            mesh.VertexArray = Enumerable.Range(0, vertexCount)
                .Select(i => new VertexPositionNormalTextureCustom { Position = new Vector4(i, 0, 0, 1) })
                .ToArray();
            mesh.IndexArray = Enumerable.Range(0, indexCount ?? vertexCount * 3)
                .Select(i => (ushort)(i % vertexCount))
                .ToArray();
            return mesh;
        }

        static Rmv2MeshNode CreateNode(string name, int vertexCount, CapabilityMaterialsEnum materialType = CapabilityMaterialsEnum.SpecGlossPbr_Default, int? indexCount = null)
        {
            var material = new Mock<IRmvMaterial>();
            material.Setup(x => x.ModelName).Returns(name);
            return new Rmv2MeshNode(CreateMesh(vertexCount, indexCount), material.Object, new CapabilityMaterialMock(materialType), null!);
        }
    }
}
