using Shared.ByteParsing;

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
            return numRtpcSize + (uint)RtpcList.Sum(rtpc => rtpc.GetSize());
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)RtpcList.Count, out _));
            foreach (var rtpc in RtpcList)
                memStream.Write(rtpc.WriteData());
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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(RtpcId, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(RtpcType, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(RtpcAccum, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(ParamId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(RtpcCurveId, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(Scaling, out _));
                memStream.Write(ByteParsers.UShort.EncodeValue((ushort)RtpcMgr.Count, out _));
                foreach (var graphPoint in RtpcMgr)
                    memStream.Write(graphPoint.WriteData());
                return memStream.ToArray();
            }

            // 4 (RtpcId) + 1 + 1 + 1 + 4 (RtpcCurveId) + 1 + 2 (Size) = 14, then the graph points.
            public uint GetSize() => 14 + (uint)RtpcMgr.Count * AkRtpcGraphPoint_V136.Size;
        }
    }
}
