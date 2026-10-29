using Shared.ByteParsing;
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

        public override byte[] WriteData()
        {
            var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.WriteData());
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)EGroupType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(GroupId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(DefaultSwitch, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BIsContinuousValidation, out _));
            memStream.Write(Children.WriteData());

            // Counts come from the lists rather than the parsed count fields, so edits keep them in step.
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)SwitchList.Count, out _));
            foreach (var switchPackage in SwitchList.Cast<CAkSwitchPackage_V136>())
                memStream.Write(switchPackage.WriteData());

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)Parameters.Count, out _));
            foreach (var parameter in Parameters)
                memStream.Write(parameter.WriteData());

            return memStream.ToArray();
        }

        public override void UpdateSectionSize()
        {
            var size = NodeBaseParams.GetSize()
                + 1 // EGroupType
                + 4 // GroupId
                + 4 // DefaultSwitch
                + 1 // BIsContinuousValidation
                + Children.GetSize()
                + 4 + (uint)SwitchList.Cast<CAkSwitchPackage_V136>().Sum(x => x.GetSize())
                + 4 + (uint)Parameters.Count * AkSwitchNodeParams_V136.Size;

            SectionSize = size + ByteHelper.GetPropertyTypeSize(Id);
        }

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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(SwitchId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)NodeIdList.Count, out _));
                foreach (var nodeId in NodeIdList)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(nodeId, out _));
                return memStream.ToArray();
            }

            public uint GetSize() => 8 + (uint)NodeIdList.Count * 4;
        }

        public class AkSwitchNodeParams_V136
        {
            public uint NodeId { get; set; }
            public byte BitVector0 { get; set; }
            public byte BitVector1 { get; set; }
            public float FadeOutTime { get; set; }
            public float FadeInTime { get; set; }

            // id + two bit vectors + two floats
            public const uint Size = 14;

            public void ReadData(ByteChunk chunk)
            {
                NodeId = chunk.ReadUInt32();
                BitVector0 = chunk.ReadByte();
                BitVector1 = chunk.ReadByte();
                FadeOutTime = chunk.ReadSingle();
                FadeInTime = chunk.ReadSingle();
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(NodeId, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(BitVector0, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(BitVector1, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(FadeOutTime, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(FadeInTime, out _));
                return memStream.ToArray();
            }
        }
    }
}
