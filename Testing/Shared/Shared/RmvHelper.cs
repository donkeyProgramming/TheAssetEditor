using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Test.TestingUtility.Shared
{
    public static class RmvHelper
    {
        public static RmvFile AssertFile(PackFile? rmv2File, RmvVersionEnum expectedVersion, uint expectedNumberOfLods, string expectedSkeletonName)
        {
            Assert.That(rmv2File, Is.Not.Null);
            var rmv2 = ModelFactory.Create().Load(rmv2File.DataSource.ReadData());

            Assert.That(rmv2.Header.Version, Is.EqualTo(expectedVersion));
            Assert.That(rmv2.Header.LodCount, Is.EqualTo(expectedNumberOfLods));
            Assert.That(rmv2.Header.SkeletonName, Is.EqualTo(expectedSkeletonName));
            return rmv2;
        }

        public static void AssertGeometryFile(RmvFile rmv2, uint expectedNumberOfLods, uint[] expectedMeshCountPerLod, uint[] vertexCountPerLod)
        {
            // Sanity checks for input
            Assert.That(expectedMeshCountPerLod.Length, Is.EqualTo(expectedNumberOfLods));
            Assert.That(vertexCountPerLod.Length, Is.EqualTo(expectedNumberOfLods));

            // Verify input file is valid
            Assert.That(rmv2, Is.Not.Null);
            Assert.That(rmv2.LodHeaders.Count, Is.EqualTo(expectedNumberOfLods));

            // Verify the geometry

            var processedMeshes = 0;
            for (var lodIndex = 0; lodIndex < expectedNumberOfLods; lodIndex++)
            {
                // verify Vertex count
                var lodVertexCount = rmv2.ModelList[lodIndex].Sum(x => x.Mesh.IndexList.Length);
                Assert.That(lodVertexCount, Is.EqualTo(vertexCountPerLod[lodIndex]), $"LodIndex:{lodIndex} - Unexpected vertex ciybt");

                // Verify number of sum meshes
                var meshesInLod = rmv2.ModelList[lodIndex].Length;
                Assert.That(meshesInLod, Is.EqualTo(expectedMeshCountPerLod[lodIndex]), $"LodIndex:{lodIndex} - Unexpected number of meshes");

                processedMeshes += rmv2.ModelList[lodIndex].Length;
            }

            // Verify all meshes are processed
            var expectedMeshCount = expectedMeshCountPerLod.Sum(x => x);
            Assert.That(processedMeshes, Is.EqualTo(expectedMeshCount));
        }

        public static void AssertMaterial(RmvFile rmv2, uint expectedNumberOfLods, VertexFormat[][] expectedVertexType, bool[][] expectedAlpha, ModelMaterialEnum expectedRmvMaterial)
        {
            // Sanity checks for input
            Assert.That(expectedAlpha.Length, Is.EqualTo(expectedNumberOfLods));
            Assert.That(expectedVertexType.Length, Is.EqualTo(expectedNumberOfLods));

            // Verify input file is valid
            Assert.That(rmv2, Is.Not.Null);
            Assert.That(rmv2.LodHeaders.Count, Is.EqualTo(expectedNumberOfLods), "Unexpected number of lods");

            // Verify the geometry
            var processedMeshes = 0;
            for (var lodIndex = 0; lodIndex < expectedNumberOfLods; lodIndex++)
            {
                processedMeshes += rmv2.ModelList[lodIndex].Length;
                for (var meshIndex = 0; meshIndex < rmv2.ModelList[lodIndex].Length; meshIndex++)
                {
                    var errorText = $"LodIndex:{lodIndex}, MeshIndex:{meshIndex}, MeshName:{rmv2.ModelList[lodIndex][meshIndex].Material.ModelName}";

                    // Verify output vertex type
                    var meshVertexType = rmv2.ModelList[lodIndex][meshIndex].Material.BinaryVertexFormat;
                    Assert.That(meshVertexType, Is.EqualTo(expectedVertexType[lodIndex][meshIndex]), "VertexType check: " + errorText);

                    // Verify alpha
                    var meshAlpha = (rmv2.ModelList[lodIndex][meshIndex].Material as WeightedMaterial).IntParams.Get(WeightedParamterIds.IntParams_Alpha_index);
                    var meshAlphaBool = meshAlpha == 1 ? true : false;
                    Assert.That(meshAlphaBool, Is.EqualTo(expectedAlpha[lodIndex][meshIndex]), "Alpha check: " + errorText);

                    // Verify material
                    var material = rmv2.ModelList[lodIndex][meshIndex].Material.MaterialId;
                    Assert.That(material, Is.EqualTo(expectedRmvMaterial), "Material check: " + errorText);
                }
            }

            // Verify all meshes are processed
            var expectedMeshCount = expectedVertexType.Sum(x => x.Length);
            Assert.That(processedMeshes, Is.EqualTo(expectedMeshCount));
        }
    }
}
