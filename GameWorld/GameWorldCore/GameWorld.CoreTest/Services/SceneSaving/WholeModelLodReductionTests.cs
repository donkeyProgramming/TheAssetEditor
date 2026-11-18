using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;
using GameWorld.Core.Test.TestUtility;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Test.Services.SceneSaving
{
    // DecimatorMeshOptimizer.GetReducedMeshCopies reduces all of a LOD's meshes together against one
    // triangle target, with whole-part removal competing with edge collapses in the same queue.
    internal class WholeModelLodReductionTests
    {
        static MeshObject CreateMesh(List<VertexPositionNormalTextureCustom> vertices, List<ushort> indices)
        {
            var mesh = new MeshObject(new TestGeometryGraphicsContextFactory().Create(), "test_skeleton")
            {
                VertexArray = vertices.ToArray(),
                IndexArray = indices.ToArray(),
            };
            mesh.ChangeVertexType(UiVertexFormat.Weighted, false);
            return mesh;
        }

        static VertexPositionNormalTextureCustom Vertex(Vector3 p, Vector2 uv) => new()
        {
            Position = new Vector4(p, 1),
            Normal = Vector3.UnitZ,
            Tangent = Vector3.UnitX,
            BiNormal = Vector3.UnitY,
            TextureCoordinate = uv,
            BlendWeights = new Vector4(1, 0, 0, 0),
        };

        // Separate flat single-quad cards (2 triangles each), spaced so none share a position
        static MeshObject CreateCards(params float[] sizes)
        {
            var vertices = new List<VertexPositionNormalTextureCustom>();
            var indices = new List<ushort>();
            for (var i = 0; i < sizes.Length; i++)
            {
                var origin = new Vector3(100 + i * 50, 0, 0);
                var first = (ushort)vertices.Count;
                foreach (var corner in new[] { Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY })
                    vertices.Add(Vertex(origin + new Vector3(corner * sizes[i], 0), corner));
                indices.AddRange([first, (ushort)(first + 1), (ushort)(first + 2), first, (ushort)(first + 2), (ushort)(first + 3)]);
            }
            return CreateMesh(vertices, indices);
        }

        static int Triangles(MeshObject mesh) => mesh.IndexArray.Length / 3;

        static float MaxCardSize(MeshObject mesh) =>
            mesh.VertexArray.Length == 0 ? 0 : mesh.VertexArray.GroupBy(v => (int)((v.Position.X - 100) / 50)).Max(g => g.Max(v => v.Position.Y));

        [Test]
        public void ReducesTheWholeModelToTheTarget_DenseMeshesMore()
        {
            var dense = DecimatorMeshOptimizerTests.CreateBumpyGrid(40);
            var sparse = DecimatorMeshOptimizerTests.CreateBumpyGrid(8);
            var total = Triangles(dense) + Triangles(sparse);

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies([dense, sparse], 0.5f);

            Assert.That(reduced, Has.Count.EqualTo(2));
            Assert.That(Triangles(reduced[0]) + Triangles(reduced[1]), Is.EqualTo(total * 0.5).Within(total * 0.02));
            var denseKept = Triangles(reduced[0]) / (double)Triangles(dense);
            var sparseKept = Triangles(reduced[1]) / (double)Triangles(sparse);
            Assert.That(denseKept, Is.LessThan(sparseKept), "The cheapest reductions are in the dense mesh");
        }

        // A large card does a lot of visual work; the budget should come out of small parts instead
        [Test]
        public void KeepsLargeCards_RemovesTinyOnes()
        {
            var tinySizes = Enumerable.Repeat(0.01f, 40);
            var cards = CreateCards([10f, .. tinySizes]);
            var body = DecimatorMeshOptimizerTests.CreateBumpyGrid(10);

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies([body, cards], 0.65f);

            Assert.That(MaxCardSize(reduced[1]), Is.EqualTo(10), "The large card is kept");
            Assert.That(Triangles(reduced[1]), Is.LessThan(Triangles(cards)), "Some tiny cards were removed");
        }

        [Test]
        public void AllCardModel_ReachesTarget_LargestCardsKept()
        {
            var sizes = Enumerable.Range(1, 50).Select(x => (float)x).ToArray();
            var cards = CreateCards(sizes);

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies([cards], 0.1f);

            Assert.That(Triangles(reduced[0]), Is.EqualTo(10), "50 cards x 2 triangles at 0.1 -> 5 cards");
            Assert.That(reduced[0].VertexArray.Select(v => v.Position.X).Distinct().Min(), Is.GreaterThanOrEqualTo(100 + 45 * 50), "The 5 largest cards are kept");
        }

        [Test]
        public void NeverRemovesTheWholeModel()
        {
            var cards = CreateCards(1, 2);

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies([cards], 0.1f);

            Assert.That(Triangles(reduced[0]), Is.GreaterThan(0));
        }

        // Meshes are separate materials: vertices in the same place in two meshes must not be merged,
        // or a collapse can give one mesh's triangles the other's UVs.
        [Test]
        public void DoesNotMergeVerticesAcrossMeshes()
        {
            var a = DecimatorMeshOptimizerTests.CreateBumpyGrid(20);
            var b = DecimatorMeshOptimizerTests.CreateBumpyGrid(20);
            for (var i = 0; i < b.VertexArray.Length; i++)
                b.VertexArray[i].TextureCoordinate += new Vector2(10, 10);     // same positions, different UV space

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies([a, b], 0.3f);

            Assert.That(reduced[0].VertexArray.All(v => v.TextureCoordinate.X <= 1.001f), "Mesh A only has mesh A's UVs");
            Assert.That(reduced[1].VertexArray.All(v => v.TextureCoordinate.X >= 9.999f), "Mesh B only has mesh B's UVs");
        }
    }
}
