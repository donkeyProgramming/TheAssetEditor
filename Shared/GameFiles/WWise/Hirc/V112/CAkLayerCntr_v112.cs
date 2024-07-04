using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V112
{
    public class CAkLayerCntr_v112 : HircItem, ICAkLayerCntr
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public List<CAkLayer> LayerList { get; set; } = new List<CAkLayer>();
        public byte bIsContinuousValidation { get; set; }
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;



        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);

            var layerCount = chunk.ReadUInt32();
            for (var i = 0; i < layerCount; i++)
                LayerList.Add(CAkLayer.Create(chunk));

            bIsContinuousValidation = chunk.ReadByte();
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();

        public List<uint> GetChildren() => Children.ChildIdList;
    }

    public class CAkLayer
    {
        public uint ulLayerID { get; set; }
        public InitialRTPC InitialRTPC { get; set; }
        public uint rtpcID { get; set; }    // Attribute name
        public AkRtpcType rtpcType { get; set; }
        public List<CAssociatedChildData> CAssociatedChildDataList { get; set; } = new List<CAssociatedChildData>();

        public static CAkLayer Create(ByteChunk chunk)
        {
            var instance = new CAkLayer();
            instance.ulLayerID = chunk.ReadUInt32();
            instance.InitialRTPC = InitialRTPC.Create(chunk);
            instance.rtpcID = chunk.ReadUInt32();
            instance.rtpcType = (AkRtpcType)chunk.ReadByte();
            var ulNumAssoc = chunk.ReadUInt32();
            for (var i = 0; i < ulNumAssoc; i++)
                instance.CAssociatedChildDataList.Add(CAssociatedChildData.Create(chunk));

            return instance;
        }
    }

    public class CAssociatedChildData
    {

        public uint ulAssociatedChildID { get; set; }
        public uint ulCurveSize { get; set; }
        public byte unknown_custom1 { get; set; }
        public List<AkRTPCGraphPoint> AkRTPCGraphPointList { get; set; } = new List<AkRTPCGraphPoint>();

        public static CAssociatedChildData Create(ByteChunk chunk)
        {
            var instance = new CAssociatedChildData();
            instance.ulAssociatedChildID = chunk.ReadUInt32();
            instance.ulCurveSize = chunk.ReadUInt32();
            for (var i = 0; i < instance.ulCurveSize; i++)
                instance.AkRTPCGraphPointList.Add(AkRTPCGraphPoint.Create(chunk));
            return instance;
        }
    }
}
