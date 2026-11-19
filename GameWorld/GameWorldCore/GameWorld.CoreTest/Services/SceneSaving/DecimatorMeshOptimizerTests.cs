using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;
using GameWorld.Core.Test.TestUtility;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Services.SceneSaving
{
    internal class DecimatorMeshOptimizerTests
    {
        // A bumpy grid has ~0.5 vertices per triangle, so a factor applied to the vertex count instead
        // of the triangle count would keep about half as many triangles as asked for.
        internal static MeshObject CreateBumpyGrid(int quadsPerSide)
        {
            var vertsPerSide = quadsPerSide + 1;
            var vertices = new VertexPositionNormalTextureCustom[vertsPerSide * vertsPerSide];
            for (var y = 0; y < vertsPerSide; y++)
            {
                for (var x = 0; x < vertsPerSide; x++)
                {
                    var height = MathF.Sin(x * 0.7f) * MathF.Cos(y * 0.9f);
                    vertices[y * vertsPerSide + x] = new VertexPositionNormalTextureCustom
                    {
                        Position = new Vector4(x, y, height, 1),
                        Normal = Vector3.UnitZ,
                        Tangent = Vector3.UnitX,
                        BiNormal = Vector3.UnitY,
                        TextureCoordinate = new Vector2((float)x / quadsPerSide, (float)y / quadsPerSide),
                        BlendWeights = new Vector4(1, 0, 0, 0),
                    };
                }
            }

            var indices = new List<ushort>();
            for (var y = 0; y < quadsPerSide; y++)
            {
                for (var x = 0; x < quadsPerSide; x++)
                {
                    var i0 = (ushort)(y * vertsPerSide + x);
                    var i1 = (ushort)(i0 + 1);
                    var i2 = (ushort)(i0 + vertsPerSide);
                    var i3 = (ushort)(i2 + 1);
                    indices.AddRange([i0, i1, i3, i0, i3, i2]);
                }
            }

            var mesh = new MeshObject(new TestGeometryGraphicsContextFactory().Create(), "test_skeleton")
            {
                VertexArray = vertices,
                IndexArray = indices.ToArray(),
            };
            mesh.ChangeVertexType(UiVertexFormat.Weighted, false);
            return mesh;
        }

        [TestCase(0.65f)]
        [TestCase(0.3f)]
        [TestCase(0.1f)]
        public void GetReducedMeshCopy_KeepsRequestedFractionOfTriangles(float factor)
        {
            var mesh = CreateBumpyGrid(30);
            var originalTriangles = mesh.IndexArray.Length / 3;

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopy(mesh, factor);

            var keptFraction = (reduced.IndexArray.Length / 3) / (double)originalTriangles;
            Assert.That(keptFraction, Is.EqualTo(factor).Within(0.05), $"Kept {keptFraction:P1} of triangles for factor {factor}");
        }

        // This head's merged hard-edge vertices give near-singular quadrics (det ~1e-17)
        [TestCase(0.65f)]
        [TestCase(0.3f)]
        public void GetReducedMeshCopies_NearSingularQuadrics_NoVertexThrownOutsideTheModel(float factor)
        {
            var path = PathHelper.GetDataFile("Karl_and_celestialgeneral_Pack//variantmeshes//wh_variantmodels//hu1e//cth//cth_celestial_general//cth_celestial_general_head_05.rigid_model_v2");
            var rmv = ModelFactory.Create().Load(File.ReadAllBytes(path));
            var builder = new MeshBuilderService(new TestGeometryGraphicsContextFactory());
            var meshes = rmv.ModelList[0].Select(m => builder.BuildMeshFromRmvModel(m, "x")).ToList();
            var positions = meshes.SelectMany(m => m.VertexArray).Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToList();
            var min = positions.Aggregate(Vector3.Min);
            var max = positions.Aggregate(Vector3.Max);
            var modelSize = Vector3.Distance(min, max);

            var reduced = DecimatorMeshOptimizer.GetReducedMeshCopies(meshes, factor);

            var furthestOutside = reduced.SelectMany(m => m.VertexArray)
                .Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z))
                .Max(p => Vector3.Distance(p, Vector3.Clamp(p, min, max)));
            Assert.That(furthestOutside / modelSize, Is.LessThan(0.01), "A vertex was thrown outside the model");
        }
    }
}
