using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkBus_v136 : HircItem
    {
        public uint OverrideBusId { get; set; }
        public uint IdDeviceShareset { get; set; }
        public BusInitialParams BusInitialParams { get; set; }

        public float RecoveryTime { get; set; }
        public float FMaxDuckVolume { get; set; }
        public DuckList DuckList { get; set; }

        public BusInitialFxParams BusInitialFxParams { get; set; }
        public byte BOverrideAttachmentParams { get; set; }
        public InitialRTPC InitialRTPC { get; set; }
        public StateChunk StateChunk { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            if (Id == 2665947595)
            {

            }

            OverrideBusId = chunk.ReadUInt32();
            if (OverrideBusId == 0)
                IdDeviceShareset = chunk.ReadUInt32();
            BusInitialParams = BusInitialParams.Create(chunk);
            RecoveryTime = chunk.ReadSingle();
            FMaxDuckVolume = chunk.ReadSingle();
            DuckList = DuckList.Create(chunk);
            BusInitialFxParams = BusInitialFxParams.Create(chunk);
            BOverrideAttachmentParams = chunk.ReadByte();
            InitialRTPC = InitialRTPC.Create(chunk);
            StateChunk = StateChunk.Create(chunk);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class DuckList
    {
        public uint UlDucks { get; set; }
        public List<AkDuckInfo> Ducks { get; set; } = [];

        public class AkDuckInfo
        {
            public uint BusID { get; set; }
            public float DuckVolume { get; set; }
            public float FadeOutTime { get; set; }
            public float FadeInTime { get; set; }
            public byte EFadeCurve { get; set; }
            public byte TargetProp { get; set; }

            public static AkDuckInfo Create(ByteChunk chunk)
            {
                var instance = new AkDuckInfo();
                instance.BusID = chunk.ReadUInt32();
                instance.DuckVolume = chunk.ReadSingle();
                instance.FadeOutTime = chunk.ReadSingle();
                instance.FadeInTime = chunk.ReadSingle();
                instance.EFadeCurve = chunk.ReadByte();
                instance.TargetProp = chunk.ReadByte();
                return instance;
            }
        }


        public static DuckList Create(ByteChunk chunk)
        {
            var instance = new DuckList();
            instance.UlDucks = chunk.ReadUInt32();
            for (uint i = 0; i < instance.UlDucks; i++)
                instance.Ducks.Add(AkDuckInfo.Create(chunk));

            return instance;
        }
    }

    public class BusInitialFxParams
    {
        public byte UNumFx { get; set; }
        public byte BitsFXBypass { get; set; }
        public List<FXChunk> FXChunkList { get; set; } = [];
        public uint FxID0 { get; set; }
        public byte bIsShareSet0 { get; set; }
        public static BusInitialFxParams Create(ByteChunk chunk)
        {
            var instance = new BusInitialFxParams();
            instance.UNumFx = chunk.ReadByte();
            if (instance.UNumFx != 0)
                instance.BitsFXBypass = chunk.ReadByte();

            for (uint i = 0; i < instance.UNumFx; i++)
                instance.FXChunkList.Add(FXChunk.Create(chunk));

            instance.FxID0 = chunk.ReadUInt32();
            instance.bIsShareSet0 = chunk.ReadByte();

            return instance;
        }
    }

    public class BusInitialParams
    {
        public AkPropBundle AkPropBundle { get; set; }
        public PositioningParams PositioningParams { get; set; }
        public AuxParams AuxParams { get; set; }
        //public byte byBitVector0 { get; set; }
        public byte ByBitVector1 { get; set; }
        public ushort U16MaxNumInstance { get; set; }
        public uint UChannelConfig { get; set; }
        public byte ByBitVector2 { get; set; }
        public static BusInitialParams Create(ByteChunk chunk)
        {
            var instance = new BusInitialParams();
            instance.AkPropBundle = AkPropBundle.Create(chunk);
            instance.PositioningParams = PositioningParams.Create(chunk);
            instance.AuxParams = AuxParams.Create(chunk);

            //instance.byBitVector0 = chunk.ReadByte();
            instance.ByBitVector1 = chunk.ReadByte();
            instance.U16MaxNumInstance = chunk.ReadUShort();
            instance.UChannelConfig = chunk.ReadUInt32();
            instance.ByBitVector2 = chunk.ReadByte();

            return instance;
        }
    }

    public class CAkAuxBus_v136 : CAkBus_v136
    {
        //public uint OverrideBusId { get; set; }
        //
        //protected override void CreateSpesificData(ByteChunk chunk)
        //{
        //    OverrideBusId = chunk.ReadUInt32();
        //    chunk.ReadBytes((int)Size - 8);
        //}
        //
        //public override void UpdateSize() => throw new NotImplementedException();
        //public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
