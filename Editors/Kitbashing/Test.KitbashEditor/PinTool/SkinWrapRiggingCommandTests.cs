using System.Reflection;
using Editors.KitbasherEditor.ChildEditors.PinTool;
using Editors.KitbasherEditor.ChildEditors.PinTool.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Moq;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Test.KitbashEditor.PinTool
{
    // Test cases:
    // 1. Execute_NotConfigured_Throws — calling Execute without Configure should throw
    // 2. Execute_SingleSource_CopiesSkeletonAndFormat — verifies skeleton name and vertex format are set from source
    // 3. Execute_VertexOnSourceVertex_GetsExactWeights — target vertex exactly on a source vertex gets that vertex's weights
    // 4. Execute_VertexBetweenTwoTriangles_GetsCloserTriangleWeights — target closer to one triangle gets its weights
    // 5. Execute_MultipleSourceMeshes_PicksClosestMesh — with two source meshes, uses the nearest one
    // 6. Execute_WeightedFormat_LimitsTo2Bones — Weighted format should keep only top 2 bone influences
    // 7. Execute_CinematicFormat_Allows4Bones — Cinematic format should keep all 4 bone influences
    // 8. Execute_MultipleTargets_AllTargetsUpdated — all target meshes get new weights
    // 9. Execute_WeightsAreNormalized — output weights sum to 1.0
    // 10. Undo_RestoresOriginalGeometry — after undo, geometry is restored to pre-execute state
    // 11. Undo_BeforeExecute_DoesNotThrow — calling undo without execute should not throw
    // 12. Execute_TargetWithPosition_AccountsForWorldSpace — target mesh position offset is applied

    [TestFixture]
    public class SkinWrapRiggingCommandTests
    {
        private Mock<SelectionManager> _selectionManagerMock = null!;
        private SkinWrapRiggingCommand _command = null!;

        [SetUp]
        public void Setup()
        {
            // SelectionManager has complex constructor and non-virtual methods;
            // use reflection to set _currentState so GetStateCopy() works.
            _selectionManagerMock = new Mock<SelectionManager>(MockBehavior.Loose,
                null!, null!, null!, null!, null!);

            var selectionState = Mock.Of<ISelectionState>(s => s.Clone() == Mock.Of<ISelectionState>());
            var field = typeof(SelectionManager).GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance)!;
            field.SetValue(_selectionManagerMock.Object, selectionState);

            _command = new SkinWrapRiggingCommand(_selectionManagerMock.Object);
        }

        [Test]
        public void Execute_NotConfigured_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _command.Execute());
        }

        [Test]
        public void Execute_SingleSource_CopiesSkeletonAndFormat()
        {
            var source = CreateMeshNode(
                position: Vector3.Zero,
                skeletonName: "humanoid01",
                format: UiVertexFormat.Cinematic,
                vertices: CreateSingleTriangleVertices(bone: 5, weight: 1.0f),
                indices: [0, 1, 2]);

            var target = CreateMeshNode(
                position: Vector3.Zero,
                skeletonName: "old_skeleton",
                format: UiVertexFormat.Static,
                vertices: [CreateVertex(0, 0, 0)],
                indices: [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            Assert.That(target.Geometry.SkeletonName, Is.EqualTo("humanoid01"));
            Assert.That(target.Geometry.VertexFormat, Is.EqualTo(UiVertexFormat.Cinematic));
        }

        [Test]
        public void Execute_VertexOnSourceVertex_GetsExactWeights()
        {
            // Source triangle with vertex at origin having bone 3, weight 1.0
            var sourceVerts = new[]
            {
                CreateVertex(0, 0, 0, boneIndices: new Vector4(3, 0, 0, 0), blendWeights: new Vector4(1, 0, 0, 0)),
                CreateVertex(1, 0, 0, boneIndices: new Vector4(3, 0, 0, 0), blendWeights: new Vector4(1, 0, 0, 0)),
                CreateVertex(0, 1, 0, boneIndices: new Vector4(3, 0, 0, 0), blendWeights: new Vector4(1, 0, 0, 0)),
            };

            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, sourceVerts, [0, 1, 2]);

            // Target vertex exactly at origin
            var targetVerts = new[] { CreateVertex(0, 0, 0) };
            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic, targetVerts, [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(3));
            Assert.That(target.Geometry.VertexArray[0].BlendWeights.X, Is.EqualTo(1.0f).Within(0.01f));
        }

        [Test]
        public void Execute_VertexBetweenTwoTriangles_GetsCloserTriangleWeights()
        {
            // Triangle 1 at z=0 with bone 1
            var v0 = CreateVertex(0, 0, 0, new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v1 = CreateVertex(2, 0, 0, new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v2 = CreateVertex(0, 2, 0, new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 0));
            // Triangle 2 at z=10 with bone 7
            var v3 = CreateVertex(0, 0, 10, new Vector4(7, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v4 = CreateVertex(2, 0, 10, new Vector4(7, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v5 = CreateVertex(0, 2, 10, new Vector4(7, 0, 0, 0), new Vector4(1, 0, 0, 0));

            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic,
                [v0, v1, v2, v3, v4, v5], [0, 1, 2, 3, 4, 5]);

            // Target vertex at z=1, much closer to triangle 1
            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.5f, 0.5f, 1)], [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(1));
        }

        [Test]
        public void Execute_MultipleSourceMeshes_PicksClosestMesh()
        {
            // Source mesh 1 at z=0, bone 2
            var source1Verts = CreateSingleTriangleVertices(bone: 2, weight: 1.0f);
            var source1 = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, source1Verts, [0, 1, 2]);

            // Source mesh 2 at z=100, bone 9
            var source2Verts = CreateSingleTriangleVertices(bone: 9, weight: 1.0f, zOffset: 100);
            var source2 = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, source2Verts, [0, 1, 2]);

            // Target vertex near origin → should pick source1 (bone 2)
            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.1f, 0.1f, 0)], [0, 0, 0]);

            _command.Configure([target], [source1, source2]);
            _command.Execute();

            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(2));
        }

        [Test]
        public void Execute_WeightedFormat_LimitsTo2Bones()
        {
            // Source triangle where each vertex has a different bone: 1, 2, 3
            var v0 = CreateVertex(0, 0, 0, new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v1 = CreateVertex(1, 0, 0, new Vector4(2, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var v2 = CreateVertex(0, 1, 0, new Vector4(3, 0, 0, 0), new Vector4(1, 0, 0, 0));
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Weighted, [v0, v1, v2], [0, 1, 2]);

            // Target vertex at center of triangle → would get influence from all 3 bones
            // but Weighted format limits to 2
            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Weighted,
                [CreateVertex(0.33f, 0.33f, 0)], [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            // Weighted format zeroes out Z and W components
            Assert.That(target.Geometry.VertexArray[0].BlendIndices.Z, Is.EqualTo(0));
            Assert.That(target.Geometry.VertexArray[0].BlendIndices.W, Is.EqualTo(0));
            Assert.That(target.Geometry.VertexArray[0].BlendWeights.Z, Is.EqualTo(0));
            Assert.That(target.Geometry.VertexArray[0].BlendWeights.W, Is.EqualTo(0));
        }

        [Test]
        public void Execute_CinematicFormat_Allows4Bones()
        {
            // Source triangle: each vertex has 2 bones, 6 unique bones total
            var v0 = CreateVertex(0, 0, 0, new Vector4(1, 2, 0, 0), new Vector4(0.5f, 0.5f, 0, 0));
            var v1 = CreateVertex(1, 0, 0, new Vector4(3, 4, 0, 0), new Vector4(0.5f, 0.5f, 0, 0));
            var v2 = CreateVertex(0, 1, 0, new Vector4(5, 6, 0, 0), new Vector4(0.5f, 0.5f, 0, 0));
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, [v0, v1, v2], [0, 1, 2]);

            // Target at triangle center
            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.33f, 0.33f, 0)], [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            var weights = target.Geometry.VertexArray[0].BlendWeights;
            var totalWeight = weights.X + weights.Y + weights.Z + weights.W;

            // Cinematic allows 4 bones, so up to 4 slots should have weight
            // At least 3 should be non-zero since all 3 vertices contribute
            var nonZeroCount = (weights.X > 0 ? 1 : 0) + (weights.Y > 0 ? 1 : 0) + (weights.Z > 0 ? 1 : 0) + (weights.W > 0 ? 1 : 0);
            Assert.That(nonZeroCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(totalWeight, Is.EqualTo(1.0f).Within(0.01f));
        }

        [Test]
        public void Execute_MultipleTargets_AllTargetsUpdated()
        {
            var sourceVerts = CreateSingleTriangleVertices(bone: 4, weight: 1.0f);
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, sourceVerts, [0, 1, 2]);

            var target1 = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.1f, 0.1f, 0)], [0, 0, 0]);
            var target2 = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.2f, 0.2f, 0)], [0, 0, 0]);

            _command.Configure([target1, target2], [source]);
            _command.Execute();

            Assert.That(target1.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(4));
            Assert.That(target2.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(4));
            Assert.That(target1.Geometry.SkeletonName, Is.EqualTo("skel"));
            Assert.That(target2.Geometry.SkeletonName, Is.EqualTo("skel"));
        }

        [Test]
        public void Execute_WeightsAreNormalized()
        {
            // Source triangle with mixed weights
            var v0 = CreateVertex(0, 0, 0, new Vector4(1, 2, 0, 0), new Vector4(0.7f, 0.3f, 0, 0));
            var v1 = CreateVertex(1, 0, 0, new Vector4(1, 3, 0, 0), new Vector4(0.6f, 0.4f, 0, 0));
            var v2 = CreateVertex(0, 1, 0, new Vector4(2, 3, 0, 0), new Vector4(0.5f, 0.5f, 0, 0));
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, [v0, v1, v2], [0, 1, 2]);

            var target = CreateMeshNode(Vector3.Zero, "old", UiVertexFormat.Cinematic,
                [CreateVertex(0.33f, 0.33f, 0)], [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            var weights = target.Geometry.VertexArray[0].BlendWeights;
            var total = weights.X + weights.Y + weights.Z + weights.W;
            Assert.That(total, Is.EqualTo(1.0f).Within(0.01f));
        }

        [Test]
        public void Undo_RestoresOriginalGeometry()
        {
            var sourceVerts = CreateSingleTriangleVertices(bone: 5, weight: 1.0f);
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, sourceVerts, [0, 1, 2]);

            var originalBoneIndex = new Vector4(99, 0, 0, 0);
            var originalWeight = new Vector4(1, 0, 0, 0);
            var targetVerts = new[] { CreateVertex(0, 0, 0, originalBoneIndex, originalWeight) };
            var target = CreateMeshNode(Vector3.Zero, "old_skel", UiVertexFormat.Cinematic, targetVerts, [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            // Verify it changed
            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(5));

            _command.Undo();

            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(99));
            Assert.That(target.Geometry.SkeletonName, Is.EqualTo("old_skel"));
        }

        [Test]
        public void Undo_BeforeExecute_DoesNotThrow()
        {
            _command.Configure([], []);
            Assert.DoesNotThrow(() => _command.Undo());
        }

        [Test]
        public void Execute_TargetWithPosition_AccountsForWorldSpace()
        {
            // Source triangle at z=10
            var sourceVerts = CreateSingleTriangleVertices(bone: 8, weight: 1.0f, zOffset: 10);
            var source = CreateMeshNode(Vector3.Zero, "skel", UiVertexFormat.Cinematic, sourceVerts, [0, 1, 2]);

            // Target vertex at local (0,0,0) but mesh position is at (0,0,10)
            // So world position is (0,0,10) which should be right on the source triangle
            var target = CreateMeshNode(new Vector3(0, 0, 10), "old", UiVertexFormat.Cinematic,
                [CreateVertex(0, 0, 0)], [0, 0, 0]);

            _command.Configure([target], [source]);
            _command.Execute();

            Assert.That(target.Geometry.VertexArray[0].BlendIndices.X, Is.EqualTo(8));
        }

        #region Helper Methods

        private static VertexPositionNormalTextureCustom CreateVertex(
            float x, float y, float z,
            Vector4? boneIndices = null,
            Vector4? blendWeights = null)
        {
            return new VertexPositionNormalTextureCustom
            {
                Position = new Vector4(x, y, z, 1),
                Normal = Vector3.Up,
                BlendIndices = boneIndices ?? Vector4.Zero,
                BlendWeights = blendWeights ?? Vector4.Zero,
            };
        }

        private static VertexPositionNormalTextureCustom[] CreateSingleTriangleVertices(int bone, float weight, float zOffset = 0)
        {
            var boneIdx = new Vector4(bone, 0, 0, 0);
            var boneWeight = new Vector4(weight, 0, 0, 0);
            return
            [
                CreateVertex(0, 0, zOffset, boneIdx, boneWeight),
                CreateVertex(1, 0, zOffset, boneIdx, boneWeight),
                CreateVertex(0, 1, zOffset, boneIdx, boneWeight),
            ];
        }

        private static Rmv2MeshNode CreateMeshNode(
            Vector3 position,
            string skeletonName,
            UiVertexFormat format,
            VertexPositionNormalTextureCustom[] vertices,
            ushort[] indices)
        {
            var contextMock = new Mock<IGraphicsCardGeometry>();
            contextMock.Setup(x => x.Clone()).Returns(() =>
            {
                var cloneMock = new Mock<IGraphicsCardGeometry>();
                cloneMock.Setup(c => c.Clone()).Returns(() => new Mock<IGraphicsCardGeometry>().Object);
                return cloneMock.Object;
            });

            var mesh = new MeshObject(contextMock.Object, skeletonName);
            mesh.VertexArray = vertices;
            mesh.IndexArray = indices;
            mesh.ChangeVertexType(format, false);

            var materialMock = new Mock<IRmvMaterial>();
            materialMock.Setup(m => m.ModelName).Returns("test_mesh");
            materialMock.Setup(m => m.PivotPoint).Returns(Vector3.Zero);
            materialMock.Setup(m => m.Clone()).Returns(() => materialMock.Object);

            var shaderMock = new Mock<CapabilityMaterial>(
                MockBehavior.Loose,
                CapabilityMaterialsEnum.SpecGlossPbr_Default,
                ShaderTypes.Pbr_SpecGloss,
                null!);

            var node = new Rmv2MeshNode(mesh, materialMock.Object, shaderMock.Object, null!);
            node.Position = position;
            return node;
        }

        #endregion
    }
}
