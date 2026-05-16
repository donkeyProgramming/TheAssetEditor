using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132.Decoding
{
    public class VorbisIdentificationHeader(WemFile wemFile)
    {
        public byte PacketType { get; } = 0x01;
        public string HeaderTag { get; } = "vorbis";
        public uint Version { get; } = 0u;
        public uint BitrateMaximum { get; } = 0u;
        public uint BitrateMinimum { get; } = 0u;
        public uint FramingBit { get; } = 1u;
        public byte Channels { get; set; } = (byte)wemFile.FmtChunk.Channels;
        public uint SampleRate { get; set; } = wemFile.FmtChunk.SampleRate;
        public uint NominalBitrate { get; set; } = wemFile.FmtChunk.AverageBytesPerSecond * BitHelper.BitsPerByte;
        public byte SmallBlockSizeExponent { get; set; } = wemFile.FmtChunk.SmallBlockSizeExponent;
        public byte LargeBlockSizeExponent { get; set; } = wemFile.FmtChunk.LargeBlockSizeExponent;

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.Byte.EncodeValue(PacketType, out _));
            stream.Write(System.Text.Encoding.ASCII.GetBytes(HeaderTag));
            stream.Write(ByteParsers.UInt32.EncodeValue(Version, out _));
            stream.Write(ByteParsers.Byte.EncodeValue(Channels, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SampleRate, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(BitrateMaximum, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(NominalBitrate, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(BitrateMinimum, out _));

            var blockSizeByte = (byte)(BitHelper.ExtractBits(SmallBlockSizeExponent, 0, 4) | (BitHelper.ExtractBits(LargeBlockSizeExponent, 0, 4) << 4));
            stream.Write(ByteParsers.Byte.EncodeValue(blockSizeByte, out _));

            var framingByte = (byte)BitHelper.ExtractBits(FramingBit, 0, 1);
            stream.Write(ByteParsers.Byte.EncodeValue(framingByte, out _));

            return stream.ToArray();
        }
    }
}
