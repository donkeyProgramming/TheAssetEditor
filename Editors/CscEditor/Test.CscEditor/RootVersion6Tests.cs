using Shared.GameFormats.Csc;
using Shared.GameFormats.Esf;

namespace Test.CscEditor
{
    // Ported from Szy-Cathay/TheAssetEditor-CN (ROOT v6 support in RootStructureDetector)
    public class RootVersion6Tests
    {
        // The v6 header as every vanilla v6 file has it, then no element groups and no attach groups
        static List<EsfNode> EmptyV6Root(EsfNode? replaceLastHeaderField = null) =>
        [
            EsfNode.Leaf(EsfNodeKind.F32, 20f),
            EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
            EsfNode.Leaf(EsfNodeKind.F32, 0f),
            EsfNode.Leaf(EsfNodeKind.Ascii, ""),
            EsfNode.Leaf(EsfNodeKind.F32, 0f),
            EsfNode.Leaf(EsfNodeKind.Ascii, ""),
            EsfNode.Leaf(EsfNodeKind.U32, 0u),
            EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
            EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
            EsfNode.Leaf(EsfNodeKind.F32, 0f),
            EsfNode.Leaf(EsfNodeKind.F32, 1f),
            replaceLastHeaderField ?? EsfNode.Leaf(EsfNodeKind.Bool, false),
            EsfNode.Leaf(EsfNodeKind.U32, 0u),
            EsfNode.Leaf(EsfNodeKind.U32, 0u),
        ];

        [Test]
        public void Version6Header_IsTwelveFields()
        {
            var structure = RootStructureDetector.Detect(EmptyV6Root(), 6);

            Assert.That(structure, Is.Not.Null);
            Assert.That(structure!.HeaderLength, Is.EqualTo(12));
        }

        [Test]
        public void Version6_WithADifferentFieldKind_IsNotRecognised()
        {
            Assert.That(RootStructureDetector.Detect(EmptyV6Root(EsfNode.Leaf(EsfNodeKind.Ascii, "x")), 6), Is.Null);
        }

        [Test]
        public void UnknownLaterVersion_IsNotRecognised()
        {
            Assert.That(RootStructureDetector.Detect(EmptyV6Root(), 7), Is.Null);
        }
    }
}
