using Shared.ByteParsing;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace GameWorld.Core.Test.GameFormats
{
    public class MetaDataFileParserTests
    {
        // Ported from Szy-Cathay/TheAssetEditor-CN 498ffa610
        [Test]
        public void TagWithNoDefinition_IsWrittenBackUnchanged()
        {
            var parser = new MetaDataFileParser(new EmptyMetaDataDatabase());
            var original = BuildMetaFile("SOME_UNKNOWN_TAG", tagVersion: 77, payload: [0x10, 0x20, 0x30, 0x40]);

            var parsed = parser.ParseFile(original);
            var written = parser.GenerateBytes(parsed.Version, parsed);

            Assert.That(parsed.Attributes[0], Is.InstanceOf<ParsedUnknownMetadataAttribute>());
            Assert.That(written, Is.EqualTo(original));
        }

        static byte[] BuildMetaFile(string tag, int tagVersion, byte[] payload)
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(2));    // file version
            data.AddRange(BitConverter.GetBytes(1));    // tag count
            data.AddRange(ByteParsers.String.WriteCaString(tag));
            data.AddRange(BitConverter.GetBytes(tagVersion));
            data.AddRange(payload);
            return data.ToArray();
        }

        sealed class EmptyMetaDataDatabase : IMetaDataDatabase
        {
            public string GetDescription(string metaDataTagName) => string.Empty;
            public string GetDescriptionSafe(string metaDataTagName) => string.Empty;
            public List<string> GetSupportedTypes() => [];
            public List<Type> GetDefinition(string metadataName) => [];
        }
    }
}
