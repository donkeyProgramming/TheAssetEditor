using NUnit.Framework;
using Shared.ByteParsing.Parsers;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class Vector3ParserTest
    {
        [Test]
        public void EncodeObject_RoundTrips_FromObject()
        {
            var parser = new Vector3Parser();
            var vec = new Vector3(1.0f, 2.0f, 3.0f);
            var bytes = parser.Encode((object)vec);

            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(12));
            Assert.That(value, Is.EqualTo(vec));
        }

        [Test]
        public void EncodeObject_RoundTrips_FromString()
        {
            var parser = new Vector3Parser();
            var str = "1|2|3";
            var bytes = parser.Encode((object)str);

            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(12));
            Assert.That(value, Is.EqualTo(new Vector3(1,2,3)));
        }
    }
}
