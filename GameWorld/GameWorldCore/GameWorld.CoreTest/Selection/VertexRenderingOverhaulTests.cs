using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Test.TestUtility;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Moq;
using GameWorld.Core.SceneNodes;
using NUnit.Framework;

namespace GameWorld.Core.Test.Selection
{
    [TestFixture]
    public class VertexRenderingOverhaulTests
    {
        static MeshObject CreateTriangleMesh()
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

        static MeshObject CreateQuadMesh()
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

        static ISelectable CreateSelectable(MeshObject mesh)
        {
            var mock = new Mock<ISelectable>();
            mock.Setup(x => x.Geometry).Returns(mesh);
            mock.Setup(x => x.RenderMatrix).Returns(Matrix.Identity);
            mock.Setup(x => x.Name).Returns("TestMesh");
            return mock.Object;
        }

        #region IntersectionMath - Screen-Space Vertex Picking

        [Test]
        public void IntersectVertex_ScreenSpace_FindsClosestVertex()
        {
            var mesh = CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            // Project vertex 0 (0,0,0) to screen to find where it should be
            var clipPos = Vector4.Transform(new Vector4(0, 0, 0, 1), viewProjection);
            var screenX = (clipPos.X / clipPos.W + 1) * 0.5f * 800;
            var screenY = (1 - clipPos.Y / clipPos.W) * 0.5f * 600;

            var result = IntersectionMath.IntersectVertex(
                new Vector2(screenX, screenY), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Not.Null);
            Assert.That(selectedVertex, Is.EqualTo(0));
        }

        [Test]
        public void IntersectVertex_ScreenSpace_ReturnsNull_WhenFarFromVertices()
        {
            var mesh = CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            // Click far from any vertex (corner of screen)
            var result = IntersectionMath.IntersectVertex(
                new Vector2(0, 0), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Null);
            Assert.That(selectedVertex, Is.EqualTo(-1));
        }

        [Test]
        public void IntersectVertex_ScreenSpace_SelectsCloserVertex()
        {
            var mesh = CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            // Project vertex 1 (1,0,0) to screen
            var clipPos = Vector4.Transform(new Vector4(1, 0, 0, 1), viewProjection);
            var screenX = (clipPos.X / clipPos.W + 1) * 0.5f * 800;
            var screenY = (1 - clipPos.Y / clipPos.W) * 0.5f * 600;

            var result = IntersectionMath.IntersectVertex(
                new Vector2(screenX, screenY), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Not.Null);
            Assert.That(selectedVertex, Is.EqualTo(1));
        }

        #endregion

        #region IntersectionMath - Edge Picking

        [Test]
        public void IntersectEdge_FindsEdge()
        {
            var mesh = CreateTriangleMesh();
            var cameraPos = new Vector3(0.5f, 0, 2);
            var ray = new Ray(cameraPos, new Vector3(0, 0, -1));

            var result = IntersectionMath.IntersectEdge(ray, mesh, cameraPos, Matrix.Identity, out var selectedEdge);

            Assert.That(result, Is.Not.Null);
            Assert.That(selectedEdge.v0, Is.GreaterThanOrEqualTo(0));
            Assert.That(selectedEdge.v1, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void IntersectEdge_ReturnsNull_WhenRayIsFar()
        {
            var mesh = CreateTriangleMesh();
            var cameraPos = new Vector3(100, 100, 2);
            var ray = new Ray(cameraPos, new Vector3(0, 0, -1));

            var result = IntersectionMath.IntersectEdge(ray, mesh, cameraPos, Matrix.Identity, out var selectedEdge);

            Assert.That(result, Is.Null);
            Assert.That(selectedEdge.v0, Is.EqualTo(-1));
        }

        [Test]
        public void IntersectEdge_OrdersEdgeVertices()
        {
            var mesh = CreateTriangleMesh();
            var cameraPos = new Vector3(0.5f, 0, 2);
            var ray = new Ray(cameraPos, new Vector3(0, 0, -1));

            IntersectionMath.IntersectEdge(ray, mesh, cameraPos, Matrix.Identity, out var selectedEdge);

            if (selectedEdge.v0 >= 0)
                Assert.That(selectedEdge.v0, Is.LessThanOrEqualTo(selectedEdge.v1));
        }

        [Test]
        public void IntersectEdges_RectangleSelection_FindsEdgesInFrustum()
        {
            var mesh = CreateTriangleMesh();
            var frustum = new BoundingFrustum(
                Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1.0f, 0.1f, 100f));

            var found = IntersectionMath.IntersectEdges(frustum, mesh, Matrix.Identity, out var edges);

            Assert.That(found, Is.True);
            Assert.That(edges.Count, Is.GreaterThan(0));
        }

        #endregion

        #region IntersectionMath - BoundingBox Helpers

        [Test]
        public void TransformBoundingBox_AppliesTranslation()
        {
            var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            var translation = Matrix.CreateTranslation(10, 0, 0);

            var result = IntersectionMath.TransformBoundingBox(box, translation);

            Assert.That(result.Min.X, Is.EqualTo(9).Within(0.001f));
            Assert.That(result.Max.X, Is.EqualTo(11).Within(0.001f));
        }

        [Test]
        public void TransformBoundingBox_AppliesScale()
        {
            var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            var scale = Matrix.CreateScale(2);

            var result = IntersectionMath.TransformBoundingBox(box, scale);

            Assert.That(result.Min.X, Is.EqualTo(-2).Within(0.001f));
            Assert.That(result.Max.X, Is.EqualTo(2).Within(0.001f));
        }

        #endregion

        #region EdgeSelectionState

        [Test]
        public void EdgeSelectionState_ModifySelection_AddsEdges()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (1, 2) }, onlyRemove: false);

            Assert.That(state.SelectionCount(), Is.EqualTo(2));
            Assert.That(state.SelectedEdges, Does.Contain((0, 1)));
            Assert.That(state.SelectedEdges, Does.Contain((1, 2)));
        }

        [Test]
        public void EdgeSelectionState_ModifySelection_RemovesEdges()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (1, 2), (0, 2) }, onlyRemove: false);
            state.ModifySelection(new[] { (1, 2) }, onlyRemove: true);

            Assert.That(state.SelectionCount(), Is.EqualTo(2));
            Assert.That(state.SelectedEdges, Does.Not.Contain((1, 2)));
        }

        [Test]
        public void EdgeSelectionState_GetSelectedVertexIndices_ReturnsUniqueVertices()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (1, 2) }, onlyRemove: false);

            var vertices = state.GetSelectedVertexIndices();

            Assert.That(vertices.Count, Is.EqualTo(3));
            Assert.That(vertices, Does.Contain(0));
            Assert.That(vertices, Does.Contain(1));
            Assert.That(vertices, Does.Contain(2));
        }

        [Test]
        public void EdgeSelectionState_Clear_RemovesAll()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (1, 2) }, onlyRemove: false);
            state.Clear();

            Assert.That(state.SelectionCount(), Is.EqualTo(0));
        }

        [Test]
        public void EdgeSelectionState_Clone_CreatesIndependentCopy()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (1, 2) }, onlyRemove: false);

            var clone = state.Clone() as EdgeSelectionState;
            clone.ModifySelection(new[] { (2, 3) }, onlyRemove: false);

            Assert.That(state.SelectionCount(), Is.EqualTo(2));
            Assert.That(clone.SelectionCount(), Is.EqualTo(3));
        }

        [Test]
        public void EdgeSelectionState_DeduplicatesEdges()
        {
            var state = new EdgeSelectionState();
            state.ModifySelection(new[] { (0, 1), (0, 1), (0, 1) }, onlyRemove: false);

            Assert.That(state.SelectionCount(), Is.EqualTo(1));
        }

        #endregion

        #region VertexSelectionState - Performance Improvements

        [Test]
        public void VertexSelectionState_WeightsZeroFalloff_OnlySelectedAreWeighted()
        {
            var mesh = CreateTriangleMesh();
            var selectable = CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);

            state.ModifySelection(new[] { 1 }, onlyRemove: false);

            Assert.That(state.VertexWeights[0], Is.EqualTo(0));
            Assert.That(state.VertexWeights[1], Is.EqualTo(1));
            Assert.That(state.VertexWeights[2], Is.EqualTo(0));
        }

        [Test]
        public void VertexSelectionState_WeightsFalloff_NearbyVerticesGetWeight()
        {
            var mesh = CreateTriangleMesh();
            var selectable = CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 2.0f);

            state.ModifySelection(new[] { 0 }, onlyRemove: false);

            Assert.That(state.VertexWeights[0], Is.EqualTo(1.0f));
            Assert.That(state.VertexWeights[1], Is.GreaterThan(0));
            Assert.That(state.VertexWeights[1], Is.LessThan(1));
            Assert.That(state.VertexWeights[2], Is.GreaterThan(0));
        }

        [Test]
        public void VertexSelectionState_ModifySelection_DeselectWorks()
        {
            var mesh = CreateTriangleMesh();
            var selectable = CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);

            state.ModifySelection(new[] { 0, 1, 2 }, onlyRemove: false);
            Assert.That(state.SelectionCount(), Is.EqualTo(3));

            state.ModifySelection(new[] { 1 }, onlyRemove: true);
            Assert.That(state.SelectionCount(), Is.EqualTo(2));
            Assert.That(state.VertexWeights[1], Is.EqualTo(0));
        }

        [Test]
        public void VertexSelectionState_Clone_IndependentCopy()
        {
            var mesh = CreateTriangleMesh();
            var selectable = CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);
            state.ModifySelection(new[] { 0 }, onlyRemove: false);

            var clone = state.Clone() as VertexSelectionState;
            clone.ModifySelection(new[] { 1 }, onlyRemove: false);

            Assert.That(state.SelectionCount(), Is.EqualTo(1));
            Assert.That(clone.SelectionCount(), Is.EqualTo(2));
        }

        [Test]
        public void VertexSelectionState_EnsureSorted_RemovesDuplicatesAndSorts()
        {
            var mesh = CreateQuadMesh();
            var selectable = CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);
            state.SelectedVertices = new List<int> { 3, 1, 1, 2 };
            state.EnsureSorted();

            Assert.That(state.SelectedVertices, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        #endregion

        #region IntersectionMath - Face/Object with BoundingBox Pre-check

        [Test]
        public void IntersectObject_ReturnNull_WhenRayMissesBoundingBox()
        {
            var mesh = CreateTriangleMesh();
            // Set a valid bounding box
            typeof(MeshObject).GetProperty("BoundingBox")?.SetValue(mesh,
                new BoundingBox(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(1.1f, 1.1f, 0.1f)));

            // Ray parallel, far away
            var ray = new Ray(new Vector3(100, 100, 5), new Vector3(0, 0, -1));
            var result = IntersectionMath.IntersectObject(ray, mesh, Matrix.Identity);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void IntersectFace_ReturnNull_WhenRayMissesBoundingBox()
        {
            var mesh = CreateTriangleMesh();
            typeof(MeshObject).GetProperty("BoundingBox")?.SetValue(mesh,
                new BoundingBox(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(1.1f, 1.1f, 0.1f)));

            var ray = new Ray(new Vector3(100, 100, 5), new Vector3(0, 0, -1));
            var result = IntersectionMath.IntersectFace(ray, mesh, Matrix.Identity, out var face);

            Assert.That(result, Is.Null);
            Assert.That(face, Is.Null);
        }

        #endregion

        #region GeometrySelectionMode Enum

        [Test]
        public void GeometrySelectionMode_ContainsEdge()
        {
            Assert.That(Enum.IsDefined(typeof(GeometrySelectionMode), GeometrySelectionMode.Edge), Is.True);
        }

        #endregion
    }
}
