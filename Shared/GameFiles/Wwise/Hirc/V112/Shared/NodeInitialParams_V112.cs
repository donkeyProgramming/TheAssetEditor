using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class NodeInitialParams_V112
    {
        public AkPropBundle_V112 AkPropBundle0 { get; set; } = new AkPropBundle_V112();
        public AkPropBundleMinMax_V112 AkPropBundle1 { get; set; } = new AkPropBundleMinMax_V112();

        public void Create(ByteChunk chunk)
        {
            AkPropBundle0.CreateSpecificData(chunk);
            AkPropBundle1.CreateSpecificData(chunk);
        }

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AkPropBundle0.GetAsBytes());
            memStream.Write(AkPropBundle1.GetAsBytes());
            return memStream.ToArray();
        }

        public uint GetSize() => AkPropBundle0.GetSize() + AkPropBundle1.GetSize();
    }
}
