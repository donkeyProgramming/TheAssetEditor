using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkRtpcGraphPoint_V136
    {
        public float From { get; set; }
        public float To { get; set; }
        public uint Interp { get; set; }

        public const uint Size = 12;

        public static AkRtpcGraphPoint_V136 ReadData(ByteChunk chunk)
        {
            return new AkRtpcGraphPoint_V136
            {
                From = chunk.ReadSingle(),
                To = chunk.ReadSingle(),
                Interp = chunk.ReadUInt32()
            };
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Single.EncodeValue(From, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(To, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(Interp, out _));
            return memStream.ToArray();
        }
    }
}
