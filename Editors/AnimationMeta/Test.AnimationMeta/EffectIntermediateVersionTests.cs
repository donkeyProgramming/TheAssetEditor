using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Test.AnimationMeta
{
    // EFFECT v6, v8, v9 and v10 decode every field and re-save byte for byte.
    // Ported from Szy-Cathay/TheAssetEditor-CN EffectIntermediateVersionTests. The payloads are built from the layouts,
    // not taken from game files.
    public class EffectIntermediateVersionTests
    {
        [TestCase(6, false)]
        [TestCase(6, true)]
        [TestCase(8, false)]
        [TestCase(8, true)]
        [TestCase(9, false)]
        [TestCase(9, true)]
        [TestCase(10, false)]
        [TestCase(10, true)]
        public void Layout_DecodesEveryField_AndRoundTrips(int version, bool tracking)
        {
            var payload = Payload(version, tracking);
            byte[] file = [.. BitConverter.GetBytes(2), .. BitConverter.GetBytes(1), .. ByteParsers.String.WriteCaString("EFFECT"), .. payload];
            var parser = new MetaDataFileParser(new MetaDataDatabase());

            var parsed = parser.ParseFile(file);
            var source = parsed.Attributes.Single();

            var expectedType = version switch { 6 => typeof(Effect_v6), 8 => typeof(Effect_v8), 9 => typeof(Effect_v9), _ => typeof(Effect_v10) };
            Assert.That(source.GetType(), Is.EqualTo(expectedType));
            var effect = (IEffectMeta)source;
            Assert.Multiple(() =>
            {
                Assert.That(effect.VfxName, Is.EqualTo("diagnostic_effect"));
                Assert.That(effect.Tracking, Is.EqualTo(tracking));
                Assert.That(effect.EffectStartTime, Is.EqualTo(.25f));
                Assert.That(effect.EffectEndTime, Is.EqualTo(2.5f));
                Assert.That(effect.Position, Is.EqualTo(new Vector3(1.5f, -2.25f, 3.75f)));
                Assert.That(effect.Orientation, Is.EqualTo(new Vector4(.25f, -.5f, .75f, 1.125f)));
                Assert.That(effect.NodeIndex, Is.EqualTo(7));
                Assert.That(source.GetType().GetProperty("Scale")!.GetValue(source), Is.EqualTo(-2.5f));
                Assert.That(source.GetType().GetProperty("TerrainMutable")!.GetValue(source), Is.True);
                Assert.That(parser.Serialize(source, out _), Is.EqualTo(payload));
                Assert.That(parser.GenerateBytes(2, parsed), Is.EqualTo(file));
            });
            if (version == 10)
                Assert.That(((Effect_v10)source).Id, Is.EqualTo(0x12345678));
            if (version == 6)
                Assert.That(source.GetType().GetProperty("DistanceCulled"), Is.Null);
            else
                Assert.That(source.GetType().GetProperty("DistanceCulled")!.GetValue(source), Is.True);
        }

        [TestCase(6)]
        [TestCase(10)]
        public void Times_ShowOnTheTimeline(int version)
        {
            var parser = new MetaDataFileParser(new MetaDataDatabase());
            var source = parser.DeSerialize(new ParsedUnknownMetadataAttribute { Name = "EFFECT", Version = version, Data = Payload(version, true) }, out var error)!;

            Assert.That(error, Is.Null);
            Assert.That(ParsedMetadataTimeRange.TryCreate(source, out var range), Is.True);
            Assert.That((range.StartTime, range.EndTime), Is.EqualTo((.25f, 2.5f)));
        }

        static byte[] Payload(int version, bool tracking)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(version));
            bytes.AddRange(BitConverter.GetBytes(.25f));
            bytes.AddRange(BitConverter.GetBytes(2.5f));
            bytes.AddRange(ByteParsers.String.WriteCaString("diagnostic_filter"));
            if (version >= 10)
                bytes.AddRange(BitConverter.GetBytes(0x12345678));
            bytes.AddRange(ByteParsers.String.WriteCaString("diagnostic_effect"));
            bytes.Add(tracking ? (byte)1 : (byte)0);
            bytes.Add(1); // TerrainMutable
            if (version > 6)
                bytes.Add(1); // DistanceCulled, from v7
            foreach (var value in new[] { 1.5f, -2.25f, 3.75f, .25f, -.5f, .75f, 1.125f })
                bytes.AddRange(BitConverter.GetBytes(value));
            bytes.AddRange(BitConverter.GetBytes(7));
            bytes.AddRange(BitConverter.GetBytes(-2.5f));
            return bytes.ToArray();
        }
    }
}
