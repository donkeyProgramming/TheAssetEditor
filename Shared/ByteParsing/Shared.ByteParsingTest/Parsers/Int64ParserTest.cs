using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class Int64ParserTest
    {
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(-1L)]
        [TestCase(1234567890123L)]
        public void EncodeValue_RoundTrips(long input)
        {
            var parser = new Int64Parser();
            var bytes = parser.EncodeValue(input, out var error);

            Assert.That(error, Is.Null);
            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes!, 0, out var value, out var bytesRead, out var decodeError);

            Assert.That(ok, Is.True);
            Assert.That(decodeError, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(value, Is.EqualTo(input));
        }

        [Test]
        public void Encode_InvalidString_ReturnsError()
        {
            var parser = new Int64Parser();
            var encoded = parser.Encode("notalong", out var error);

            Assert.That(encoded, Is.Null);
            Assert.That(error, Is.Not.Null);
        }
    }
}
