using E2EVerification.Shared;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Half = SharpDX.Half;
using SharpDX.X3DAudio;
using Shared.GameFormats.RigidModel.Vertex;

namespace E2EVerification
{
    internal class VertexPresicion_GenerateTest
    {
        [Test]        
        [TestCase(-1.1123f, 0.422123f, 0.00213f, (ushort)47300, (ushort)13116, (ushort)5292, (ushort)16248)]
        [TestCase(-110.0001f, 13213.0f, -10.00213f, (ushort)54791, (ushort)29096, (ushort)51298, (ushort)15504)]
        [TestCase(1.0f, 2.0f, 3.0f, (ushort)15360, (ushort)16384, (ushort)16896, (ushort)15360)]
        [TestCase(0.0001f, -1200.0f, 0.00213f, (ushort)894, (ushort)57600, (ushort)5287, (ushort)16256)]
        [TestCase(12340.1123f, -11.422123f, -0.00123f, (ushort)28825, (ushort)51291, (ushort)37808, (ushort)15678)]
        public void DoesCodeProduceTheExpectedVertex(float x, float y, float z, ushort x2, ushort y2, ushort z2, ushort w2)
        {
            var halfVertex = VertexLoadHelper.ConvertertVertexToHalfExtraPrecision(new Vector4(x, y, z, 0));                        
            Assert.That(x2 == halfVertex.X.RawValue && y2 == halfVertex.Y.RawValue && z2 == halfVertex.Z.RawValue && w2 == halfVertex.W.RawValue);
        }
    }
}
