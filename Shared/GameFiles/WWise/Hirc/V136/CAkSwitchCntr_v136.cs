﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkSwitchCntr_v136 : HircItem, INodeBaseParamsAccessor, ICAkSwitchCntr
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public AkGroupType EGroupType { get; set; }
        public uint UlGroupID { get; set; }   // Enum group name
        public uint UlDefaultSwitch { get; set; }    // Default value name
        public byte BIsContinuousValidation { get; set; }
        public Children Children { get; set; }
        public List<ICAkSwitchCntr.ICAkSwitchPackage> SwitchList { get; set; } = [];
        public List<AkSwitchNodeParams> Parameters { get; set; } = [];
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;


        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            EGroupType = (AkGroupType)chunk.ReadByte();
            UlGroupID = chunk.ReadUInt32();
            UlDefaultSwitch = chunk.ReadUInt32();
            BIsContinuousValidation = chunk.ReadByte();
            Children = Children.Create(chunk);

            var switchListCount = chunk.ReadUInt32();
            for (var i = 0; i < switchListCount; i++)
                SwitchList.Add(CAkSwitchPackage.Create(chunk));

            var paramCount = chunk.ReadUInt32();
            for (var i = 0; i < paramCount; i++)
                Parameters.Add(AkSwitchNodeParams.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();

        public uint GroupId { get => UlGroupID; }
        public uint DefaultSwitch { get => UlDefaultSwitch; }
    }
}
