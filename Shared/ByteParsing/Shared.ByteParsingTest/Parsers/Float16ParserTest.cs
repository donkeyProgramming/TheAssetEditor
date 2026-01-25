using NUnit.Framework;
using Shared.ByteParsing.Parsers;
using SharpDX;
using Half = SharpDX.Half;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class Float16ParserTest
    {
        [TestCase(0.0f)]
        [TestCase(1.0f)]
        [TestCase(-1.0f)]
        [TestCase(123.456f)]
        public void EncodeValue_RoundTrips(float input)
        {
            var parser = new Float16Parser();
            var bytes = parser.EncodeValue(new Half(input), out var error);

            Assert.That(error, Is.Null);
            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes!, 0, out var value, out var bytesRead, out var decodeError);

            Assert.That(ok, Is.True);
            Assert.That(decodeError, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(2));
            // Compare approximate since Half precision is limited
            Assert.That((float)value, Is.EqualTo(input).Within(0.01f));
        }

        [Test]
        public void Encode_InvalidString_ReturnsError()
        {
            var parser = new Float16Parser();
            var encoded = parser.Encode("notafloat", out var error);

            Assert.That(encoded, Is.Null);
            Assert.That(error, Is.Not.Null);
        }
    }
}
