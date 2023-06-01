using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkMusicSegment_v136 : HircItem, INodeBaseParamsAccessor
    {

        public MusicNodeParams MusicNodeParams { get; set; }
        public double fDuration { get; set; }
        public List<AkMusicMarkerWwise> pArrayMarkersList { get; set; } = new List<AkMusicMarkerWwise>();

        public NodeBaseParams NodeBaseParams => MusicNodeParams.NodeBaseParams;

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            MusicNodeParams = MusicNodeParams.Create(chunk);

            fDuration = chunk.ReadInt64(); //chunk.ReadDouble();

            var ulNumMarkers = chunk.ReadUInt32();
            for (int i = 0; i < ulNumMarkers; i++)
                pArrayMarkersList.Add(AkMusicMarkerWwise.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkMusicMarkerWwise
    {
        public uint id { get; set; }
        public double fPosition { get; set; }

        //see below
        //public string pMarkerName { get; set; }
        public List<byte> pMarkerName { get; set; } = new List<byte>();

        public static AkMusicMarkerWwise Create(ByteChunk chunk)
        {
            var instance = new AkMusicMarkerWwise();
            instance.id = chunk.ReadUInt32();
            instance.fPosition = chunk.ReadInt64(); //chunk.ReadDouble();

            //instance.pMarkerName = chunk.ReadString();
            //The above wasn't working because uStringSize is an uint32, yet the ReadString was trying to read it as a uint16
            //So instead I just made it read the raw bytes, stored in a list
            var uStringSize = chunk.ReadUInt32();
            for (int i = 0; i < uStringSize; i++)
                instance.pMarkerName.Add(chunk.ReadByte());

            return instance;
        }
    }

    public class MusicNodeParams
    {
        public byte uFlags { get; set; }
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public AkMeterInfo AkMeterInfo { get; set; }
        public byte bMeterInfoFlag { get; set; }
        public List<CAkStinger> pStingersList { get; set; } = new List<CAkStinger>();

        public static MusicNodeParams Create(ByteChunk chunk)
        {
            var instance = new MusicNodeParams();
            instance.uFlags = chunk.ReadByte();
            instance.NodeBaseParams = NodeBaseParams.Create(chunk);
            instance.Children = Children.Create(chunk);
            instance.AkMeterInfo = AkMeterInfo.Create(chunk);
            instance.bMeterInfoFlag = chunk.ReadByte();

            var NumStingers = chunk.ReadUInt32();
            for (int i = 0; i < NumStingers; i++)
                instance.pStingersList.Add(CAkStinger.Create(chunk));

            return instance;
        }
    }

    public class AkMeterInfo
    {
        public double fGridPeriod { get; set; }
        public double fGridOffset { get; set; }
        public float fTempo { get; set; }
        public byte uTimeSigNumBeatsBar { get; set; }
        public byte uTimeSigBeatValue { get; set; }

        public static AkMeterInfo Create(ByteChunk chunk)
        {
            var instance = new AkMeterInfo();
            instance.fGridPeriod = chunk.ReadInt64(); //chunk.ReadDouble();
            instance.fGridOffset = chunk.ReadInt64(); //chunk.ReadDouble();
            instance.fTempo = chunk.ReadSingle();
            instance.uTimeSigNumBeatsBar = chunk.ReadByte();
            instance.uTimeSigBeatValue = chunk.ReadByte();

            return instance;
        }
    }

    public class CAkStinger
    {
        public uint TriggerID { get; set; }
        public uint SegmentID { get; set; }
        public uint SyncPlayAt { get; set; }
        public uint uCueFilterHash { get; set; }
        public int DontRepeatTime { get; set; }
        public uint numSegmentLookAhead { get; set; }

        public static CAkStinger Create(ByteChunk chunk)
        {
            var instance = new CAkStinger();
            instance.TriggerID = chunk.ReadUInt32();
            instance.SegmentID = chunk.ReadUInt32();
            instance.SyncPlayAt = chunk.ReadUInt32();
            instance.uCueFilterHash = chunk.ReadUInt32();
            instance.DontRepeatTime = chunk.ReadInt32();
            instance.numSegmentLookAhead = chunk.ReadUInt32();

            return instance;
        }
    }
}