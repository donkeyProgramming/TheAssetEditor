using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise;
using System;
using System.Collections.Generic;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkBus_v136 : HircItem
    {
        public uint OverrideBusId { get; set; }
        public uint idDeviceShareset { get; set; }
        public BusInitialParams BusInitialParams { get; set; }

        public float RecoveryTime { get; set; }
        public float fMaxDuckVolume { get; set; }
        public DuckList DuckList { get; set; }

        public BusInitialFxParams BusInitialFxParams { get; set; }
        public byte bOverrideAttachmentParams { get; set; }
        public InitialRTPC InitialRTPC { get; set; }
        public StateChunk StateChunk { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            if (Id == 2665947595)
            {

            }

            OverrideBusId = chunk.ReadUInt32();
            if (OverrideBusId == 0)
                idDeviceShareset = chunk.ReadUInt32();
            BusInitialParams = BusInitialParams.Create(chunk);
            RecoveryTime = chunk.ReadSingle();
            fMaxDuckVolume = chunk.ReadSingle();
            DuckList = DuckList.Create(chunk);
            BusInitialFxParams = BusInitialFxParams.Create(chunk);
            bOverrideAttachmentParams = chunk.ReadByte();
            InitialRTPC = InitialRTPC.Create(chunk);
            StateChunk = StateChunk.Create(chunk);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class DuckList
    {
        public uint ulDucks { get; set; }
        public List<AkDuckInfo> Ducks { get; set; } = new List<AkDuckInfo>();

        public class AkDuckInfo
        {
            public uint BusID { get; set; }
            public float DuckVolume { get; set; }
            public float FadeOutTime { get; set; }
            public float FadeInTime { get; set; }
            public byte eFadeCurve { get; set; }
            public byte TargetProp { get; set; }

            public static AkDuckInfo Create(ByteChunk chunk)
            {
                var instance = new AkDuckInfo();
                instance.BusID = chunk.ReadUInt32();
                instance.DuckVolume = chunk.ReadSingle();
                instance.FadeOutTime = chunk.ReadSingle();
                instance.FadeInTime = chunk.ReadSingle();
                instance.eFadeCurve = chunk.ReadByte();
                instance.TargetProp = chunk.ReadByte();
                return instance;
            }
        }


        public static DuckList Create(ByteChunk chunk)
        {
            var instance = new DuckList();
            instance.ulDucks = chunk.ReadUInt32();
            for (uint i = 0; i < instance.ulDucks; i++)
                instance.Ducks.Add(AkDuckInfo.Create(chunk));


            return instance;
        }
    }

    public class BusInitialFxParams
    {
        public byte uNumFx { get; set; }
        public byte bitsFXBypass { get; set; }
        public List<FXChunk> FXChunkList { get; set; } = new List<FXChunk>();
        public uint fxID_0 { get; set; }
        public byte bIsShareSet_0 { get; set; }
        public static BusInitialFxParams Create(ByteChunk chunk)
        {
            var instance = new BusInitialFxParams();
            instance.uNumFx = chunk.ReadByte();
            if (instance.uNumFx != 0)
                instance.bitsFXBypass = chunk.ReadByte();

            for (uint i = 0; i < instance.uNumFx; i++)
                instance.FXChunkList.Add(FXChunk.Create(chunk));

            instance.fxID_0 = chunk.ReadUInt32();
            instance.bIsShareSet_0 = chunk.ReadByte();

            return instance;
        }
    }

    public class BusInitialParams
    {
        public AkPropBundle AkPropBundle { get; set; }
        public PositioningParams PositioningParams { get; set; }
        public AuxParams AuxParams { get; set; }
        //public byte byBitVector0 { get; set; }
        public byte byBitVector1 { get; set; }
        public ushort u16MaxNumInstance { get; set; }
        public uint uChannelConfig { get; set; }
        public byte byBitVector2 { get; set; }
        public static BusInitialParams Create(ByteChunk chunk)
        {
            var instance = new BusInitialParams();
            instance.AkPropBundle = AkPropBundle.Create(chunk);
            instance.PositioningParams = PositioningParams.Create(chunk);
            instance.AuxParams = AuxParams.Create(chunk);

            //instance.byBitVector0 = chunk.ReadByte();
            instance.byBitVector1 = chunk.ReadByte();
            instance.u16MaxNumInstance = chunk.ReadUShort();
            instance.uChannelConfig = chunk.ReadUInt32();
            instance.byBitVector2 = chunk.ReadByte();

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
