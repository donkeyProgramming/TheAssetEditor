using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkLayerCntr_v136 : HircItem, INodeBaseParamsAccessor, ICAkLayerCntr
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public List<CAkLayer> LayerList { get; set; } = [];
        public byte BIsContinuousValidation { get; set; }
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);

            var layerCount = chunk.ReadUInt32();
            for (var i = 0; i < layerCount; i++)
                LayerList.Add(CAkLayer.Create(chunk));

            BIsContinuousValidation = chunk.ReadByte();
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();

        public List<uint> GetChildren() => Children.ChildIdList;
    }

    public class CAkLayer
    {
        public uint UlLayerID { get; set; }
        public InitialRTPC InitialRTPC { get; set; }
        public uint RtpcID { get; set; }    // Attribute name
        public AkRtpcType RtpcType { get; set; }
        public List<CAssociatedChildData> CAssociatedChildDataList { get; set; } = [];

        public static CAkLayer Create(ByteChunk chunk)
        {
            var instance = new CAkLayer();
            instance.UlLayerID = chunk.ReadUInt32();
            instance.InitialRTPC = InitialRTPC.Create(chunk);
            instance.RtpcID = chunk.ReadUInt32();
            instance.RtpcType = (AkRtpcType)chunk.ReadByte();
            var ulNumAssoc = chunk.ReadUInt32();
            for (var i = 0; i < ulNumAssoc; i++)
                instance.CAssociatedChildDataList.Add(CAssociatedChildData.Create(chunk));

            return instance;
        }
    }

    public class CAssociatedChildData
    {

        public uint UlAssociatedChildID { get; set; }
        public byte Unknown_custom0 { get; set; }
        public byte Unknown_custom1 { get; set; }
        public List<AkRTPCGraphPoint> AkRTPCGraphPointList { get; set; } = [];

        public static CAssociatedChildData Create(ByteChunk chunk)
        {
            var instance = new CAssociatedChildData();
            instance.UlAssociatedChildID = chunk.ReadUInt32();
            instance.Unknown_custom0 = chunk.ReadByte();
            instance.Unknown_custom1 = chunk.ReadByte();
            var pointCount = chunk.ReadUInt32();
            for (var i = 0; i < pointCount; i++)
                instance.AkRTPCGraphPointList.Add(AkRTPCGraphPoint.Create(chunk));
            return instance;
        }
    }
}
