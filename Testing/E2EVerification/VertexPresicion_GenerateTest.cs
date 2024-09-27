using System.Numerics;
using Shared.GameFormats.RigidModel.Vertex;
using Half4 = SharpDX.Half4;

namespace E2EVerification
{
    internal class VertexPresicion_GenerateTest
    {        
        [Test] public void EncodeVertexTest_1() => EncodeVertexTest(new Vector3(-1.1123f, 0.422123f, 0.00213f), new Half4(-0.5957031f, 0.22607422f, 0.0011405945f, 1.8671875f));
        [Test] public void EncodeVertexTest_2() => EncodeVertexTest(new Vector3(-110.0001f, 13213.0f, -10.00213f), new Half4(-96.4375f, 11584f, -8.765625f, 1.140625f));
        [Test] public void EncodeVertexTest_3() => EncodeVertexTest(new Vector3(1.0123f, 12.0f, 333.0f), new Half4(0.6748047f, 8f, 222f, 1.5f));
        [Test] public void EncodeVertexTest_4() => EncodeVertexTest(new Vector3(0.0001f, -1200.0f, 0.00213f), new Half4(5.3286552E-05f, -640f, 0.0011358261f,1.875f));
        [Test] public void EncodeVertexTest_5() => EncodeVertexTest(new Vector3(12340.1123f, -11.422123f, -0.00563f), new Half4(9416f, -8.7109375f, -0.004295349f, 1.3105469f));
        private void EncodeVertexTest(Vector3 input, Half4 expectedEncodedVertex)
        {
            var actualEncodedVertex = VertexLoadHelper.ConvertertVertexToHalfExtraPrecision(new Vector4(input.X, input.Y, input.Z, 0));            

            Assert.That(actualEncodedVertex.W, Is.EqualTo(expectedEncodedVertex.W));
            Assert.That(actualEncodedVertex.X, Is.EqualTo(expectedEncodedVertex.X));
            Assert.That(actualEncodedVertex.Y, Is.EqualTo(expectedEncodedVertex.Y));
            Assert.That(actualEncodedVertex.Z, Is.EqualTo(expectedEncodedVertex.Z));            
        }
    }
}
