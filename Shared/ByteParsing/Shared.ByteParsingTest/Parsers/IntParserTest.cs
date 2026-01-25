using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class IntParserTest
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(123456)]
        public void EncodeValue_RoundTrips(int input)
        {
            var parser = new IntParser();
            var bytes = parser.EncodeValue(input, out var error);

            Assert.That(error, Is.Null);
            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes!, 0, out var value, out var bytesRead, out var decodeError);

            Assert.That(ok, Is.True);
            Assert.That(decodeError, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(value, Is.EqualTo(input));
        }

        [Test]
        public void Encode_InvalidString_ReturnsError()
        {
            var parser = new IntParser();
            var encoded = parser.Encode("notanint", out var error);

            Assert.That(encoded, Is.Null);
            Assert.That(error, Is.Not.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(123456)]
        public void TryDecodeValue_Succeeds_ForValidBytes(int input)
        {
            var parser = new IntParser();
            var bytes = parser.EncodeValue(input, out var encodeError);

            Assert.That(encodeError, Is.Null);

            var ok = parser.TryDecodeValue(bytes!, 0, out var value, out var bytesRead, out var decodeError);

            Assert.That(ok, Is.True);
            Assert.That(decodeError, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(value, Is.EqualTo(input));
        }

        [Test]
        public void TryDecodeValue_Fails_WhenBufferTooSmall()
        {
            var parser = new IntParser();
            var buffer = new byte[3];

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(bytesRead, Is.EqualTo(0));
        }
    }
}
