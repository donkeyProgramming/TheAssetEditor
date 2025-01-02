using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkSound_V112 : HircItem, ICAkSound
    {
        public AkBankSourceData_V112 AkBankSourceData { get; set; }
        public NodeBaseParams_V112 NodeBaseParams { get; set; } = new NodeBaseParams_V112();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            AkBankSourceData = AkBankSourceData_V112.Create(chunk);
            NodeBaseParams.Create(chunk);
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(AkBankSourceData.GetAsByteArray());
            memStream.Write(NodeBaseParams.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkSound_V112();
            sanityReload.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            var akBankSourceDataSize = AkBankSourceData.GetSize();
            var nodeBaseParamsSize = NodeBaseParams.GetSize();
            SectionSize = idSize + akBankSourceDataSize + nodeBaseParamsSize;
        }

        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
        public uint GetSourceId() => AkBankSourceData.AkMediaInformation.SourceId;
        public AKBKSourceType GetStreamType() => AkBankSourceData.StreamType;
    }
}
