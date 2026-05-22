using NUnit.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsingTest.Parsers
{
    [TestFixture]
    public class SingleParserTest
    {
        [TestCase(0.0f)]
        [TestCase(1.5f)]
        [TestCase(-2.25f)]
        public void EncodeObject_RoundTrips(float input)
        {
            var parser = new SingleParser();
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
