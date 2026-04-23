using GameWorld.Core.Components.Selection;
using GameWorld.Core.Test.TestUtility;

namespace GameWorld.Core.Test.Selection
{
    [TestFixture]
    public class VertexSelectionStateTests
    {
        [Test]
        public void WeightsZeroFalloff_OnlySelectedAreWeighted()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var selectable = MeshTestHelper.CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);

            state.ModifySelection(new[] { 1 }, onlyRemove: false);

            Assert.That(state.VertexWeights[0], Is.EqualTo(0));
            Assert.That(state.VertexWeights[1], Is.EqualTo(1));
            Assert.That(state.VertexWeights[2], Is.EqualTo(0));
        }

        [Test]
        public void WeightsFalloff_NearbyVerticesGetWeight()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var selectable = MeshTestHelper.CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 2.0f);

            state.ModifySelection(new[] { 0 }, onlyRemove: false);

            Assert.That(state.VertexWeights[0], Is.EqualTo(1.0f));
            Assert.That(state.VertexWeights[1], Is.GreaterThan(0));
            Assert.That(state.VertexWeights[1], Is.LessThan(1));
            Assert.That(state.VertexWeights[2], Is.GreaterThan(0));
        }

        [Test]
        public void ModifySelection_DeselectWorks()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var selectable = MeshTestHelper.CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);

            state.ModifySelection(new[] { 0, 1, 2 }, onlyRemove: false);
            Assert.That(state.SelectionCount(), Is.EqualTo(3));

            state.ModifySelection(new[] { 1 }, onlyRemove: true);
            Assert.That(state.SelectionCount(), Is.EqualTo(2));
            Assert.That(state.VertexWeights[1], Is.EqualTo(0));
        }

        [Test]
        public void Clone_IndependentCopy()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var selectable = MeshTestHelper.CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);
            state.ModifySelection(new[] { 0 }, onlyRemove: false);

            var clone = state.Clone() as VertexSelectionState;
            clone.ModifySelection(new[] { 1 }, onlyRemove: false);

            Assert.That(state.SelectionCount(), Is.EqualTo(1));
            Assert.That(clone.SelectionCount(), Is.EqualTo(2));
        }

        [Test]
        public void EnsureSorted_RemovesDuplicatesAndSorts()
        {
            var mesh = MeshTestHelper.CreateQuadMesh();
            var selectable = MeshTestHelper.CreateSelectable(mesh);
            var state = new VertexSelectionState(selectable, 0);
            state.SelectedVertices = new List<int> { 3, 1, 1, 2 };
            state.EnsureSorted();

            Assert.That(state.SelectedVertices, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }
    }
}
