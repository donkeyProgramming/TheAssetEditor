using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class PluginPropertyValue_V136
    {
        public uint PropertyId { get; set; }
        public byte RtpcAccum { get; set; }
        public float Value { get; set; }

        public static PluginPropertyValue_V136 ReadData(ByteChunk chunk)
        {
            var instance = new PluginPropertyValue_V136();
            instance.PropertyId = chunk.ReadUInt32();
            instance.RtpcAccum = chunk.ReadByte();
            instance.Value = chunk.ReadSingle();
            return instance;
        }
    }
}
