using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkSound_V112 : HircItem, ICAkSound
    {
        public AkBankSourceData_V112 AkBankSourceData { get; set; }
        public NodeBaseParams_V112 NodeBaseParams { get; set; } = new NodeBaseParams_V112();

        protected override void ReadData(ByteChunk chunk)
        {
            AkBankSourceData = AkBankSourceData_V112.ReadData(chunk);
            NodeBaseParams.ReadData(chunk);
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(AkBankSourceData.WriteData());
            memStream.Write(NodeBaseParams.WriteData());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkSound_V112();
            sanityReload.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(ID);
            var akBankSourceDataSize = AkBankSourceData.GetSize();
            var nodeBaseParamsSize = NodeBaseParams.GetSize();
            SectionSize = idSize + akBankSourceDataSize + nodeBaseParamsSize;
        }

        public uint GetDirectParentID() => NodeBaseParams.DirectParentId;
        public uint GetSourceID() => AkBankSourceData.AkMediaInformation.SourceId;
        public AKBKSourceType GetStreamType() => AkBankSourceData.StreamType;
    }
}
