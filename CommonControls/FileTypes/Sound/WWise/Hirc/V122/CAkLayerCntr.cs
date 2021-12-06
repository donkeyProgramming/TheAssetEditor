using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V122
{
    public class CAkLayerCntr : HircItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public List<CAkLayer> LayerList { get; set; } = new List<CAkLayer>();
        public byte bIsContinuousValidation { get; set; }

        protected override void Create(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);

            var layerCount = chunk.ReadUInt32();
            for (int i = 0; i < layerCount; i++)
                LayerList.Add(CAkLayer.Create(chunk));

            bIsContinuousValidation = chunk.ReadByte();
        }
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
            for (int i = 0; i < ulNumAssoc; i++)
                instance.CAssociatedChildDataList.Add(CAssociatedChildData.Create(chunk));

            return instance;
        }
    }

    public class CAssociatedChildData
    {

        public uint ulAssociatedChildID { get; set; }
        public byte unknown_custom0 { get; set; }
        public byte unknown_custom1 { get; set; }
        public List<AkRTPCGraphPoint> AkRTPCGraphPointList { get; set; } = new List<AkRTPCGraphPoint>();

        public static CAssociatedChildData Create(ByteChunk chunk)
        {
            var instance = new CAssociatedChildData();
            instance.ulAssociatedChildID = chunk.ReadUInt32();
            instance.unknown_custom0 = chunk.ReadByte();
            instance.unknown_custom1 = chunk.ReadByte();
            var pointCount = chunk.ReadUInt32();
            for (int i = 0; i < pointCount; i++)
                instance.AkRTPCGraphPointList.Add(AkRTPCGraphPoint.Create(chunk));
            return instance;
        }
    }
}
