using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{

    public class CAkSwitchCntr_v136 : HircItem, INodeBaseParamsAccessor
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public AkGroupType eGroupType { get; set; }
        public uint ulGroupID { get; set; }   // Enum group name
        public uint ulDefaultSwitch { get; set; }    // Default value name
        public byte bIsContinuousValidation { get; set; }
        public Children Children { get; set; }
        public List<CAkSwitchPackage> SwitchList { get; set; } = new List<CAkSwitchPackage>();
        public List<AkSwitchNodeParams> Parameters { get; set; } = new List<AkSwitchNodeParams>();

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            eGroupType = (AkGroupType)chunk.ReadByte();
            ulGroupID = chunk.ReadUInt32();
            ulDefaultSwitch = chunk.ReadUInt32();
            bIsContinuousValidation = chunk.ReadByte();
            Children = Children.Create(chunk);

            var switchListCount = chunk.ReadUInt32();
            for (int i = 0; i < switchListCount; i++)
                SwitchList.Add(CAkSwitchPackage.Create(chunk));

            var paramCount = chunk.ReadUInt32();
            for (int i = 0; i < paramCount; i++)
                Parameters.Add(AkSwitchNodeParams.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}