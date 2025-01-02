using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkSound_v136 : HircItem, ICAkSound
    {
        public AkBankSourceData_V136 AkBankSourceData { get; set; }
        public NodeBaseParams_V136 NodeBaseParams { get; set; } = new NodeBaseParams_V136();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            AkBankSourceData = AkBankSourceData_V136.Create(chunk);
            NodeBaseParams.Create(chunk);
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(AkBankSourceData.GetAsByteArray());
            memStream.Write(NodeBaseParams.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkSound_v136();
            sanityReload.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            SectionSize = idSize + AkBankSourceData.GetSize() + NodeBaseParams.GetSize();
        }

        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
        public uint GetSourceId() => AkBankSourceData.AkMediaInformation.SourceId;
        public AKBKSourceType GetStreamType() => AkBankSourceData.StreamType;
    }
}
