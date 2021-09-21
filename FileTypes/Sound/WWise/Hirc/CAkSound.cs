using Filetypes.ByteParsing;
using System;

namespace FileTypes.Sound.WWise.Hirc
{
    public class CAkSound : HricItem
    {

        public AkBankSourceData BankSourceData { get; set; }
        public NodeBaseParams NodeBaseParams { get; set; }

        public static CAkSound Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var sound = new CAkSound();
            sound.LoadCommon(chunk);
            sound.BankSourceData = AkBankSourceData.Create(chunk);
            sound.NodeBaseParams = NodeBaseParams.Create(chunk);

            sound.SkipToEnd(chunk, objectStartIndex + 5);
            return sound;

        }
    }

    public class AkBankSourceData
    {
        public uint PluginId { get; set; }
        public ushort PluginId_type { get; set; }
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation akMediaInformation { get; set; }
        public uint uSize { get; set; }
        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                //PluginId_type = chunk.ReadUShort(),
                //PluginId_company = chunk.ReadUShort(),
                StreamType = (SourceType)chunk.ReadByte()
            };

         
            output.PluginId_type = (ushort)((output.PluginId >> 0) & 0x000F);
            output.PluginId_company = (ushort)((output.PluginId >> 4) & 0x03FF);

            if (output.StreamType != SourceType.Straming)
            {
             //   throw new Exception();
            }

            if (output.PluginId_type == 0x02)
                output.uSize = chunk.ReadUInt32();

            output.akMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }
    }


    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint uInMemoryMediaSize { get; set; }
        public byte uSourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk)
        {
            return new AkMediaInformation()
            {
                SourceId = chunk.ReadUInt32(),
                uInMemoryMediaSize = chunk.ReadUInt32(),
                uSourceBits = chunk.ReadByte(),
            };
        }
    }
}
