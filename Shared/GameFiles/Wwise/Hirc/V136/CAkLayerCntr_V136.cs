using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkLayerCntr_V136 : HircItem, ICAkLayerCntr
    {
        public NodeBaseParams_V136 NodeBaseParams { get; set; } = new NodeBaseParams_V136();
        public Children_V136 Children { get; set; } = new Children_V136();
        public uint NumLayers { get; set; }
        public List<CAkLayer_V136> LayerList { get; set; } = [];
        public byte IsContinuousValidation { get; set; }

        protected override void ReadData(ByteChunk chunk)
        {
            NodeBaseParams.ReadData(chunk);
            Children.ReadData(chunk);

            NumLayers = chunk.ReadUInt32();
            for (var i = 0; i < NumLayers; i++)
            {
                var layer = new CAkLayer_V136();
                layer.ReadData(chunk);
                LayerList.Add(layer);
            }

            IsContinuousValidation = chunk.ReadByte();
        }

        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");

        public List<uint> GetChildren() => Children.ChildIds;
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;

        public class CAkLayer_V136
        {
            public uint LayerId { get; set; }
            public InitialRtpc_V136 InitialRtpc { get; set; } = new InitialRtpc_V136();
            public uint RtpcId { get; set; }
            public AkRtpcType RtpcType { get; set; }
            public uint NumAssoc { get; set; }
            public List<CAssociatedChildData_V136> CAssociatedChildDataList { get; set; } = [];

            public void ReadData(ByteChunk chunk)
            {
                LayerId = chunk.ReadUInt32();
                InitialRtpc.ReadData(chunk);
                RtpcId = chunk.ReadUInt32();
                RtpcType = (AkRtpcType)chunk.ReadByte();

                NumAssoc = chunk.ReadUInt32();
                for (var i = 0; i < NumAssoc; i++)
                {
                    var associatedChildData = new CAssociatedChildData_V136();
                    associatedChildData.ReadData(chunk);
                    CAssociatedChildDataList.Add(associatedChildData);
                }
            }
        }

        public class CAssociatedChildData_V136
        {
            public uint AssociatedChildId { get; set; }
            public byte UnknownCustom0 { get; set; }
            public byte UnknownCustom1 { get; set; }
            public uint CurveSize {  get; set; }
            public List<AkRtpcGraphPoint_V136> AkRtpcGraphPointList { get; set; } = [];

            public void ReadData(ByteChunk chunk)
            {
                AssociatedChildId = chunk.ReadUInt32();
                UnknownCustom0 = chunk.ReadByte();
                UnknownCustom1 = chunk.ReadByte();
                CurveSize = chunk.ReadUInt32();
                for (var i = 0; i < CurveSize; i++)
                    AkRtpcGraphPointList.Add(AkRtpcGraphPoint_V136.ReadData(chunk));
            }
        }
    }
}
