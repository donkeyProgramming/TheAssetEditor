using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.ICAkSwitchCntr;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkSwitchCntr_V136 : HircItem, ICAkSwitchCntr
    {
        public NodeBaseParams_V136 NodeBaseParams { get; set; } = new NodeBaseParams_V136();
        public AkGroupType EGroupType { get; set; }
        public uint GroupId { get; set; }
        public uint DefaultSwitch { get; set; }
        public byte BIsContinuousValidation { get; set; }
        public Children_V136 Children { get; set; } = new Children_V136();
        public uint NumSwitchGroups { get; set; }
        public List<ICAkSwitchPackage> SwitchList { get; set; } = [];
        public uint NumSwitchParams { get; set; }
        public List<AkSwitchNodeParams_V136> Parameters { get; set; } = [];
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;

        protected override void ReadData(ByteChunk chunk)
        {
            NodeBaseParams.ReadData(chunk);
            EGroupType = (AkGroupType)chunk.ReadByte();
            GroupId = chunk.ReadUInt32();
            DefaultSwitch = chunk.ReadUInt32();
            BIsContinuousValidation = chunk.ReadByte();
            Children.ReadData(chunk);

            NumSwitchGroups = chunk.ReadUInt32();
            for (var i = 0; i < NumSwitchGroups; i++)
            {
                var cAkSwitchPackage = new CAkSwitchPackage_V136();
                cAkSwitchPackage.ReadData(chunk);
                SwitchList.Add(cAkSwitchPackage);
            }

            NumSwitchParams = chunk.ReadUInt32();
            for (var i = 0; i < NumSwitchParams; i++)
            {
                var akSwitchNoteParams = new AkSwitchNodeParams_V136();
                akSwitchNoteParams.ReadData(chunk);
                Parameters.Add(akSwitchNoteParams);
            }
        }

        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");

        public class CAkSwitchPackage_V136 : ICAkSwitchPackage
        {
            public uint SwitchId { get; set; }
            public List<uint> NodeIdList { get; set; } = [];

            public void ReadData(ByteChunk chunk)
            {
                SwitchId = chunk.ReadUInt32();
                var numChildren = chunk.ReadUInt32();
                for (var i = 0; i < numChildren; i++)
                    NodeIdList.Add(chunk.ReadUInt32());
            }
        }

        public class AkSwitchNodeParams_V136
        {
            public uint NodeId { get; set; }
            public byte BitVector0 { get; set; }
            public byte BitVector1 { get; set; }
            public float FadeOutTime { get; set; }
            public float FadeInTime { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                NodeId = chunk.ReadUInt32();
                BitVector0 = chunk.ReadByte();
                BitVector1 = chunk.ReadByte();
                FadeOutTime = chunk.ReadSingle();
                FadeInTime = chunk.ReadSingle();
            }
        }
    }
}
