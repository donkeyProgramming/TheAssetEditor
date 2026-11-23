using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Vertex;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class TangentBasisCalculatorTests
{
    [Test]
    public void CalculateForRmv2Mesh_DegenerateUv_KeepsFiniteBasis()
    {
        var mesh = new RmvMesh
        {
            IndexList = [0, 1, 2],
            VertexList =
            [
                CreateVertex(new Vector4(0, 0, 0, 1)),
                CreateVertex(new Vector4(1, 0, 0, 1)),
                CreateVertex(new Vector4(0, 1, 0, 1)),
            ],
        };

        TangentBasisCalculator.CalculateForRmv2Mesh(mesh);

        foreach (var vertex in mesh.VertexList)
        {
            Assert.Multiple(() =>
            {
                Assert.That(IsFinite(vertex.Tangent), Is.True);
                Assert.That(IsFinite(vertex.BiNormal), Is.True);
            });
        }
    }

    private static CommonVertex CreateVertex(Vector4 position) => new()
    {
        Position = position,
        Normal = Vector3.UnitZ,
        Tangent = Vector3.Zero,
        BiNormal = Vector3.Zero,
        Uv = Vector2.Zero,
    };

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}
