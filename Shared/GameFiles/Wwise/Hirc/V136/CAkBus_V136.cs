using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkBus_V136 : HircItem
    {
        public uint OverrideBusId { get; set; }
        public uint IdDeviceShareset { get; set; }
        public BusInitialParams_V136 BusInitialParams { get; set; } = new BusInitialParams_V136();
        public float RecoveryTime { get; set; }
        public float MaxDuckVolume { get; set; }
        public DuckList_V136 DuckList { get; set; } = new DuckList_V136();
        public BusInitialFxParams_V136 BusInitialFxParams { get; set; } = new BusInitialFxParams_V136();
        public byte OverrideAttachmentParams { get; set; }
        public InitialRtpc_V136 InitialRtpc { get; set; } = new InitialRtpc_V136();
        public StateChunk_V136 StateChunk { get; set; } = new StateChunk_V136();

        protected override void ReadData(ByteChunk chunk)
        {
            OverrideBusId = chunk.ReadUInt32();
            if (OverrideBusId == 0)
                IdDeviceShareset = chunk.ReadUInt32();
            BusInitialParams.ReadData(chunk);
            RecoveryTime = chunk.ReadSingle();
            MaxDuckVolume = chunk.ReadSingle();
            DuckList.ReadData(chunk);
            BusInitialFxParams.ReadData(chunk);
            OverrideAttachmentParams = chunk.ReadByte();
            InitialRtpc.ReadData(chunk);
            StateChunk.ReadData(chunk);
        }

        // We don't need to make CAkBus objects because we can route audio through the existing busses as hircs appear to be shared between Banks.
        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");

        public class BusInitialParams_V136
        {
            public AkPropBundle_V136 AkPropBundle { get; set; } = new AkPropBundle_V136();
            public PositioningParams_V136 PositioningParams { get; set; } = new PositioningParams_V136();
            public AuxParams_V136 AuxParams { get; set; } = new AuxParams_V136();
            public byte BitVector1 { get; set; }
            public ushort MaxNumInstance { get; set; }
            public uint ChannelConfig { get; set; }
            public byte BitVector2 { get; set; }
            public void ReadData(ByteChunk chunk)
            {
                AkPropBundle.ReadData(chunk);
                PositioningParams.ReadData(chunk);
                AuxParams.ReadData(chunk);
                BitVector1 = chunk.ReadByte();
                MaxNumInstance = chunk.ReadUShort();
                ChannelConfig = chunk.ReadUInt32();
                BitVector2 = chunk.ReadByte();
            }
        }

        public class DuckList_V136
        {
            public uint UlDucks { get; set; }
            public List<AkDuckInfo_V136> Ducks { get; set; } = [];

            public class AkDuckInfo_V136
            {
                public uint BusId { get; set; }
                public float DuckVolume { get; set; }
                public float FadeOutTime { get; set; }
                public float FadeInTime { get; set; }
                public byte FadeCurve { get; set; }
                public byte TargetProp { get; set; }

                public void ReadData(ByteChunk chunk)
                {
                    BusId = chunk.ReadUInt32();
                    DuckVolume = chunk.ReadSingle();
                    FadeOutTime = chunk.ReadSingle();
                    FadeInTime = chunk.ReadSingle();
                    FadeCurve = chunk.ReadByte();
                    TargetProp = chunk.ReadByte();
                }
            }

            public void ReadData(ByteChunk chunk)
            {
                UlDucks = chunk.ReadUInt32();
                for (uint i = 0; i < UlDucks; i++)
                {
                    var akDuckInfo = new AkDuckInfo_V136();
                    akDuckInfo.ReadData(chunk);
                    Ducks.Add(akDuckInfo);
                }
            }
        }

        public class BusInitialFxParams_V136
        {
            public byte NumFx { get; set; }
            public byte BitsFxBypass { get; set; }
            public List<FxChunk_V136> FxChunk { get; set; } = [];
            public uint FxId0 { get; set; }
            public byte IsShareSet0 { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                NumFx = chunk.ReadByte();
                if (NumFx != 0)
                    BitsFxBypass = chunk.ReadByte();

                for (uint i = 0; i < NumFx; i++)
                    FxChunk.Add(FxChunk_V136.ReadData(chunk));

                FxId0 = chunk.ReadUInt32();
                IsShareSet0 = chunk.ReadByte();
            }
        }
    }
}
