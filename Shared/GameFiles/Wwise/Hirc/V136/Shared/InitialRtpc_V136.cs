using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class InitialRtpc_V136
    {
        public ushort NumRtpc { get; set; }
        public List<Rtpc_V136> RtpcList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            NumRtpc = chunk.ReadUShort();
            for (var i = 0; i < NumRtpc; i++)
            {
                var rtpc = new Rtpc_V136();
                rtpc.ReadData(chunk);
                RtpcList.Add(rtpc);
            }
        }

        public uint GetSize()
        {
            var numRtpcSize = ByteHelper.GetPropertyTypeSize(NumRtpc);
            if (RtpcList.Count != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");
            return numRtpcSize;
        }

        public byte[] WriteData()
        {
            if (RtpcList.Count != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)RtpcList.Count, out _));
            return memStream.ToArray();
        }

        public class Rtpc_V136
        {
            public uint RtpcId { get; set; }
            public byte RtpcType { get; set; }
            public byte RtpcAccum { get; set; }
            public byte ParamId { get; set; }
            public uint RtpcCurveId { get; set; }
            public byte Scaling { get; set; }
            public ushort Size { get; set; }
            public List<AkRtpcGraphPoint_V136> RtpcMgr { get; set; } = [];

            public void ReadData(ByteChunk chunk)
            {
                RtpcId = chunk.ReadUInt32();
                RtpcType = chunk.ReadByte();
                RtpcAccum = chunk.ReadByte();
                ParamId = chunk.ReadByte();
                RtpcCurveId = chunk.ReadUInt32();
                Scaling = chunk.ReadByte();
                Size = chunk.ReadUShort();
                for (var i = 0; i < Size; i++)
                    RtpcMgr.Add(AkRtpcGraphPoint_V136.ReadData(chunk));
            }
        }
    }
}
