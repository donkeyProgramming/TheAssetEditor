using NUnit.Framework;
using Shared.ByteParsing.Parsers;
using System.Text;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class FixedAciiStringParserTest
    {
        [Test]
        public void TryDecodeValue_Succeeds_ForValidAscii()
        {
            var parser = new FixedAciiStringParser(4);
            var buffer = Encoding.ASCII.GetBytes("ABCD");

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(value, Is.EqualTo("ABCD"));
        }

        [Test]
        public void TryDecodeValue_Fails_WhenBufferTooSmall()
        {
            var parser = new FixedAciiStringParser(4);
            var buffer = new byte[3];

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(bytesRead, Is.EqualTo(0));
        }

        [Test]
        public void Encode_Throws_NotImplemented()
        {
            var parser = new FixedAciiStringParser(4);
            Assert.Throws<System.Exception>(() => parser.Encode((object)"ABCD"));
        }
    }
}
