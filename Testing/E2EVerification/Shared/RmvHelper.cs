using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;

namespace E2EVerification.Shared
{
    internal static class RmvHelper
    {
        public static void AssertFile(PackFile rmv2File, uint[] expectedMeshCountPerLod, uint[] vertexCount, VertexFormat[][] expectedVertexType, AlphaMode[][] expectedAlpha)
        {
            // Sanity checks for input
            Assert.That(expectedMeshCountPerLod.Length, Is.EqualTo(vertexCount.Length));
            Assert.That(expectedMeshCountPerLod.Length, Is.EqualTo(expectedVertexType.Length));
            Assert.That(expectedMeshCountPerLod.Length, Is.EqualTo(expectedAlpha.Length));

            // Verify input file is valid
            Assert.That(rmv2File, Is.Not.Null);

            var rmv2 = ModelFactory.Create().Load(rmv2File.DataSource.ReadData());
            Assert.That(rmv2.LodHeaders.Count, Is.EqualTo(4));

            // Verify the geometry
            var expectedMeshCount = expectedMeshCountPerLod.Sum(x => x);
            var processedMeshes = 0;
            for (var lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                // verify Vertex count
                var lodVertexCount = rmv2.ModelList[lodIndex].Sum(x => x.Mesh.IndexList.Length);
                Assert.That(lodVertexCount, Is.EqualTo(vertexCount[lodIndex]));

                // Verify number of sum meshes
                var meshesInLod = rmv2.ModelList[lodIndex].Length;
                Assert.That(meshesInLod, Is.EqualTo(expectedMeshCountPerLod[lodIndex]));

                for (var meshIndex = 0; meshIndex < rmv2.ModelList[lodIndex].Length; meshIndex++)
                {
                    processedMeshes++;

                    // Verify output vertex type
                    var meshVertexType = rmv2.ModelList[lodIndex][meshIndex].Material.BinaryVertexFormat;
                    Assert.That(expectedVertexType[lodIndex][meshIndex], Is.EqualTo(meshVertexType));

                    // Verify alpha
                    var meshAlpha = rmv2.ModelList[lodIndex][meshIndex].Material.AlphaMode;
                    Assert.That(expectedAlpha[lodIndex][meshIndex], Is.EqualTo(meshAlpha));
                }
            }

            // Verify all meshes are processed
            Assert.That(processedMeshes, Is.EqualTo(expectedMeshCount));
        }
    }
}
