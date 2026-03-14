using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class UIntParserTest
    {
        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(123456u)]
        public void EncodeObject_RoundTrips(uint input)
        {
            var parser = new UIntParser();
            var bytes = parser.Encode((object)input);

            Assert.That(bytes, Is.Not.Null);

            var ok = parser.TryDecodeValue(bytes, 0, out var value, out var bytesRead, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(value, Is.EqualTo(input));
        }
    }
}
