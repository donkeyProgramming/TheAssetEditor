using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel.Vertex.Formats;

namespace GameWorld.Core.Test.GameFormats;

public class RigidModelVertexLayoutTests
{
    [TestCase(VertexFormat.Sway, 20u, typeof(SwayVertexLayout))]
    [TestCase(VertexFormat.Sway, 16u, typeof(Position16_bitVertexCreator))]
    [TestCase(VertexFormat.CustomTerrain, 60u, typeof(VegetationVertexLayout))]
    [TestCase(VertexFormat.CustomTerrain, 36u, typeof(CustomTerrainVertexCreator))]
    [TestCase(VertexFormat.Position16_bit, 28u, typeof(GrassVertexLayout))]
    [TestCase(VertexFormat.Position16_bit, 8u, typeof(PositionHalfVertexLayout))]
    [TestCase(VertexFormat.Position16_bit, 16u, typeof(Position16_bitVertexCreator))]
    [TestCase(VertexFormat.Rome2Tree, 28u, typeof(TreeBillboardVertexLayout))]
    [TestCase(VertexFormat.Rome2Tree, 12u, typeof(PositionUvVertexLayout))]
    [TestCase(VertexFormat.Rome2Water, 12u, typeof(PositionUvVertexLayout))]
    [TestCase(VertexFormat.Unknown, 28u, typeof(StaticNoUv2VertexLayout))]
    [TestCase(VertexFormat.Unknown, 24u, typeof(BowWaveVertexLayout))]
    public void Layout_IsResolvedFromDeclaredIdAndStride(VertexFormat declared, uint stride, Type expected)
    {
        Assert.That(VertexFactory.Create().ResolveLayout(declared, stride, RmvVersionEnum.RMV2_V7), Is.TypeOf(expected));
    }

    [Test]
    public void UnmatchedLayout_IsNotResolved()
    {
        Assert.That(VertexFactory.Create().ResolveLayout(VertexFormat.Sway, 44, RmvVersionEnum.RMV2_V7), Is.Null);
    }

    static IEnumerable<IVertexCreator> Layouts() =>
    [
        new SwayVertexLayout(), new VegetationVertexLayout(), new GrassVertexLayout(), new PositionHalfVertexLayout(),
        new TreeBillboardVertexLayout(), new PositionUvVertexLayout(VertexFormat.Rome2Water), new StaticNoUv2VertexLayout(), new BowWaveVertexLayout(),
    ];

    static CommonVertex SampleVertex(int extraCount) => new()
    {
        Position = new Vector4(1.25f, -0.5f, 2.0f, 1),
        Normal = Vector3.Normalize(new Vector3(0.2f, 0.9f, -0.1f)),
        Tangent = Vector3.UnitX,
        BiNormal = Vector3.UnitZ,
        Uv = new Vector2(0.25f, 0.75f),
        Colour = new Vector4(1, 0.5f, 0, 191 / 255.0f),
        BoneIndex = [],
        BoneWeight = [],
        Extra = Enumerable.Range(0, extraCount).Select(i => i * 0.5f).ToArray(),
    };

    // Writing, reading back and writing again must give the same bytes, so a mesh survives any number of re-saves
    [TestCaseSource(nameof(Layouts))]
    public void Layout_WritesBackWhatItReads(IVertexCreator layout)
    {
        var extraCount = layout switch { VegetationVertexLayout => 12, TreeBillboardVertexLayout => 4, BowWaveVertexLayout => 5, PositionHalfVertexLayout => 1, _ => 0 };
        var bytes = layout.Write(RmvVersionEnum.RMV2_V7, SampleVertex(extraCount));

        var read = layout.ReadArray(RmvVersionEnum.RMV2_V7, bytes, 0, bytes.Length, 1)[0];

        Assert.That(bytes, Has.Length.EqualTo(layout.GetVertexSize(RmvVersionEnum.RMV2_V7)));
        Assert.That(layout.Write(RmvVersionEnum.RMV2_V7, read), Is.EqualTo(bytes));
        Assert.That(read.GetPosistionAsVec3().X, Is.EqualTo(1.25f));
        Assert.That(read.Extra, Has.Length.EqualTo(extraCount));
    }

    [Test]
    public void Sway_CarriesTheUvInThePositionAndNormalW_AndDoesNotScaleThePosition()
    {
        var bytes = new SwayVertexLayout().Write(RmvVersionEnum.RMV2_V7, SampleVertex(0));
        var read = new SwayVertexLayout().ReadArray(RmvVersionEnum.RMV2_V7, bytes, 0, 20, 1)[0];

        Assert.That(read.Position, Is.EqualTo(new Vector4(1.25f, -0.5f, 2.0f, 1)));
        Assert.That(read.Uv, Is.EqualTo(new Vector2(0.25f, 0.75f)));
        Assert.That(bytes[19], Is.EqualTo(191), "the colour's alpha is the sway weight");
    }

    [Test]
    public void PositionHalf_KeepsWAsAChannel_NotAScale()
    {
        var vertex = SampleVertex(0);
        vertex.Extra = [0.5f];
        var bytes = new PositionHalfVertexLayout().Write(RmvVersionEnum.RMV2_V6, vertex);
        var read = new PositionHalfVertexLayout().ReadArray(RmvVersionEnum.RMV2_V6, bytes, 0, 8, 1)[0];

        Assert.That(read.GetPosistionAsVec3(), Is.EqualTo(new Vector3(1.25f, -0.5f, 2.0f)));
        Assert.That(read.Extra, Is.EqualTo(new[] { 0.5f }));
    }

    [Test]
    public void Position16_KeepsAWThatIsNotOne()
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(2.0f).CopyTo(bytes, 0);
        BitConverter.GetBytes(0.5f).CopyTo(bytes, 12);
        var creator = new Position16_bitVertexCreator();

        var read = creator.ReadArray(RmvVersionEnum.RMV2_V6, bytes, 0, 16, 1)[0];

        Assert.That(read.Position.X, Is.EqualTo(1.0f), "W is a scale on XYZ");
        Assert.That(creator.Write(RmvVersionEnum.RMV2_V6, read), Is.EqualTo(bytes));
    }
}
