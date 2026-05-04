using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Test.TestUtility;
using Microsoft.Xna.Framework;
using Moq;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using System.Reflection;
using Test.GameWorld.Core.WsMaterialTemplate;

namespace GameWorld.Core.Test.Selection
{
    // This test is terrible! Keeping it so that I know what to change later 

    [TestFixture]
    public class SelectionManagerTests
    {
        private Mock<IEventHub> _eventHubMock;
#pragma warning disable NUnit1032 // RenderEngine constructed with null deps cannot be Disposed safely
        private RenderEngineComponent _renderEngine;
#pragma warning restore NUnit1032
        private SelectionManager _selectionManager;

        [TearDown]
        public void TearDown()
        {
            _selectionManager?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _eventHubMock = new Mock<IEventHub>();

            _renderEngine = new RenderEngineComponent(null, null, null, null, null, null, _eventHubMock.Object, null);

            _selectionManager = new SelectionManager(
                _eventHubMock.Object,
                _renderEngine,
                null,
                null,
                null,
                new ApplicationSettingsService());

            // Set render items that are normally created in Initialize() (requires GPU)
            SetPrivateField(_selectionManager, "_edgeQuadRenderItem", new EdgeQuadRenderItem());
            SetPrivateField(_selectionManager, "_vertexRenderItem", new VertexRenderItem());
        }

        [Test]
        public void Draw_SwitchFromLargeMeshToSmallMesh_NoIndexOutOfRange()
        {
            // Arrange: mesh A with 20 vertices, select vertex 15
            var nodeA = CreateMeshNode(vertexCount: 20);
            var stateA = new VertexSelectionState(nodeA, 0);
            stateA.ModifySelection(new[] { 15 }, onlyRemove: false);
            _selectionManager.SetState(stateA);
            _selectionManager.Draw(new GameTime());

            // Switch to mesh B with only 3 vertices, no selection
            var nodeB = CreateMeshNode(vertexCount: 3);
            var stateB = new VertexSelectionState(nodeB, 0);
            _selectionManager.SetState(stateB);

            // Act & Assert: stale _sampleIdx0=15 must not cause IndexOutOfRangeException
            Assert.DoesNotThrow(() => _selectionManager.Draw(new GameTime()));
        }

        [Test]
        public void Draw_ZeroSelectedVerticesAfterHighIndexSelection_NoIndexOutOfRange()
        {
            // Arrange: mesh with 20 vertices, select high-index vertices
            var node = CreateMeshNode(vertexCount: 20);
            var state = new VertexSelectionState(node, 0);
            state.ModifySelection(new[] { 15, 16 }, onlyRemove: false);
            _selectionManager.SetState(state);
            _selectionManager.Draw(new GameTime());

            // Deselect everything — stale indices persist with same mesh
            state.ModifySelection(new[] { 15, 16 }, onlyRemove: true);

            // Act & Assert: stale indices are still valid for same mesh
            Assert.DoesNotThrow(() => _selectionManager.Draw(new GameTime()));
        }

        [Test]
        public void Draw_SwitchFromLargeMeshToSmallMesh_IndicesAreClamped()
        {
            // Arrange: mesh A with 20 vertices, select vertex 18
            var nodeA = CreateMeshNode(vertexCount: 20);
            var stateA = new VertexSelectionState(nodeA, 0);
            stateA.ModifySelection(new[] { 18, 19 }, onlyRemove: false);
            _selectionManager.SetState(stateA);
            _selectionManager.Draw(new GameTime());

            // Switch to mesh B with only 3 vertices, select vertex 2
            var nodeB = CreateMeshNode(vertexCount: 3);
            var stateB = new VertexSelectionState(nodeB, 0);
            stateB.ModifySelection(new[] { 2 }, onlyRemove: false);
            _selectionManager.SetState(stateB);

            // Act & Assert: should handle the index clamping safely
            Assert.DoesNotThrow(() => _selectionManager.Draw(new GameTime()));
        }

        private static Rmv2MeshNode CreateMeshNode(int vertexCount)
        {
            var contextFactory = new TestGeometryGraphicsContextFactory();
            var mesh = new MeshObject(contextFactory.Create(), "test_skeleton");
            mesh.VertexArray = new VertexPositionNormalTextureCustom[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                mesh.VertexArray[i] = new VertexPositionNormalTextureCustom { Position = new Vector4(i, 0, 0, 1) };

            // Build a valid triangle index array
            var indices = new List<ushort>();
            for (var i = 0; i < vertexCount - 2; i++)
                indices.AddRange(new ushort[] { 0, (ushort)(i + 1), (ushort)(i + 2) });
            mesh.IndexArray = indices.ToArray();

            var materialMock = new Mock<IRmvMaterial>();
            materialMock.Setup(m => m.ModelName).Returns("test_mesh");
            materialMock.Setup(m => m.PivotPoint).Returns(Vector3.Zero);

            var shader = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);

            return new Rmv2MeshNode(mesh, materialMock.Object, shader, null);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(obj, value);
        }
    }
}
