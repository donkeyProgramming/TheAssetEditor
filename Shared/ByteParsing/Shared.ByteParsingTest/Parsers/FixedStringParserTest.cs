using NUnit.Framework;
using Shared.ByteParsing.Parsers;
using System.Text;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class FixedStringParserTest
    {
        [Test]
        public void TryDecodeValue_Succeeds_ForValidUnicode()
        {
            var parser = new FixedStringParser(4);
            var buffer = Encoding.Unicode.GetBytes("ABCD");

            var ok = parser.TryDecodeValue(buffer, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(value, Is.EqualTo("ABCD"));
        }

        [Test]
        public void Encode_Throws_NotImplemented()
        {
            var parser = new FixedStringParser(4);
            Assert.Throws<System.Exception>(() => parser.Encode((object)"ABCD"));
        }
    }
}
