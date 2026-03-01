using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class UShortParserTest
    {
        [TestCase((ushort)0)]
        [TestCase((ushort)1)]
        [TestCase((ushort)65535)]
        public void EncodeObject_RoundTrips(ushort input)
        {
            var parser = new UShortParser();
            var bytes = parser.Encode((object)input);

            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(input));
        }
    }
}
