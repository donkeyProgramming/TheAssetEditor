using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class ByteParserTest
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(32)]
        [TestCase(200)]
        [TestCase(255)]
        public void EncodeValue_ReturnsExpectedByte(int input)
        {
            var parser = new ByteParser();
            var bytes = parser.EncodeValue((byte)input, out var error);

            Assert.That(error, Is.Null);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes!.Length, Is.EqualTo(1));
            Assert.That(bytes[0], Is.EqualTo((byte)input));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(128)]
        [TestCase(255)]
        public void TryDecodeValue_Succeeds_ForValidByte(int input)
        {
            var parser = new ByteParser();
            var buffer = new byte[] { (byte)input };

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(value, Is.EqualTo((byte)input));
            Assert.That(bytesRead, Is.EqualTo(1));
        }

        [Test]
        public void TryDecodeValue_Fails_WhenBufferTooSmall()
        {
            var parser = new ByteParser();
            var buffer = new byte[0];

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(bytesRead, Is.EqualTo(0));
        }
    }
}
