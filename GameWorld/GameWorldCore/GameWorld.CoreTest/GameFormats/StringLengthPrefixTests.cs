using Shared.ByteParsing;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;

namespace GameWorld.Core.Test.GameFormats
{
    // Ported from Szy-Cathay/TheAssetEditor-CN 5a3df1313
    public class StringLengthPrefixTests
    {
        const string NonAscii = "狮鹫骑士_é";

        [Test]
        public void Utf8String_RoundTrips()
        {
            var bytes = ByteParsers.String.WriteCaString(NonAscii);

            Assert.That(BitConverter.ToInt16(bytes, 0), Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(NonAscii)));
            Assert.That(ByteParsers.String.TryDecode(bytes, 0, out var value, out var bytesRead, out _), Is.True);
            Assert.That(value, Is.EqualTo(NonAscii));
            Assert.That(bytesRead, Is.EqualTo(bytes.Length));
        }

        [Test]
        public void Utf16String_StillStoresCharacterCount()
        {
            // StringAscii is the UTF-16 parser despite its name
            var bytes = ByteParsers.StringAscii.WriteCaString(NonAscii);

            Assert.That(BitConverter.ToInt16(bytes, 0), Is.EqualTo(NonAscii.Length));
            Assert.That(ByteParsers.StringAscii.TryDecode(bytes, 0, out var value, out _, out _), Is.True);
            Assert.That(value, Is.EqualTo(NonAscii));
        }

        [Test]
        public void Utf8OptString_RoundTrips()
        {
            var bytes = ByteParsers.OptString.WriteCaString(NonAscii);

            Assert.That(ByteParsers.OptString.TryDecode(bytes, 0, out var value, out _, out _), Is.True);
            Assert.That(value, Is.EqualTo(NonAscii));
        }

        [Test]
        public void DatFile_NonAsciiEventName_RoundTrips()
        {
            var file = new SoundDatFile();
            file.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = NonAscii, Value = 1.5f });

            var bytes = DatFileParser.WriteData(file);
            var reread = DatFileParser.Parse(PackFile.CreateFromBytes("test.dat", bytes), false);

            Assert.That(reread.EventWithStateGroup[0].Event, Is.EqualTo(NonAscii));
            Assert.That(reread.EventWithStateGroup[0].Value, Is.EqualTo(1.5f));
        }
    }
}
