using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class NodeInitialParams_V136
    {
        public AkPropBundle_V136 AkPropBundle0 { get; set; } = new AkPropBundle_V136();
        public AkPropBundleMinMax_V136 AkPropBundle1 { get; set; } = new AkPropBundleMinMax_V136();

        public void ReadData(ByteChunk chunk)
        {
            AkPropBundle0.ReadData(chunk);
            AkPropBundle1.ReadData(chunk);
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AkPropBundle0.WriteData());
            memStream.Write(AkPropBundle1.WriteData());
            return memStream.ToArray();
        }

        public uint GetSize() => AkPropBundle0.GetSize() + AkPropBundle1.GetSize();
    }
}
