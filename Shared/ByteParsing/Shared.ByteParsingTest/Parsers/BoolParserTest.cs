using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class BoolParserTest
    {
        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void EncodeValue_ReturnsExpectedByte(bool input, int expectedByte)
        {
            var parser = new BoolParser();
            var bytes = parser.EncodeValue(input, out var error);

            Assert.That(error, Is.Null, "Expected no error from EncodeValue");
            Assert.That(bytes, Is.Not.Null, "EncodeValue returned null");
            Assert.That(bytes!.Length, Is.EqualTo(1), "Encoded byte length");
            Assert.That(bytes[0], Is.EqualTo((byte)expectedByte));
        }

        [TestCase(1, true)]
        [TestCase(0, false)]
        public void TryDecodeValue_Succeeds_ForValidByte(int inputByte, bool expected)
        {
            var parser = new BoolParser();
            var buffer = new byte[] { (byte)inputByte };

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True, "Expected TryDecodeValue to succeed");
            Assert.That(error, Is.Null, "Expected no error");
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(bytesRead, Is.EqualTo(1));
        }

        [TestCase(2)]
        [TestCase(255)]
        public void TryDecodeValue_Fails_ForInvalidByte(int inputByte)
        {
            var parser = new BoolParser();
            var buffer = new byte[] { (byte)inputByte };

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.False, "Expected TryDecodeValue to fail for invalid bool byte");
            Assert.That(error, Is.Not.Null, "Expected an error message");
            Assert.That(bytesRead,Is.EqualTo(0));
        }
    }
}
