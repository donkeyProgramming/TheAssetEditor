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
            var writer = new BitWriter(64);
            writer.WriteByte(PacketType);
            writer.WriteAscii(HeaderTag);
            writer.WriteBits(Version, 32);
            writer.WriteByte(Channels);
            writer.WriteBits(SampleRate, 32);
            writer.WriteBits(BitrateMaximum, 32);
            writer.WriteBits(NominalBitrate, 32);
            writer.WriteBits(BitrateMinimum, 32);
            writer.WriteBits(SmallBlockSizeExponent, 4);
            writer.WriteBits(LargeBlockSizeExponent, 4);
            writer.WriteBits(FramingBit, 1);
            writer.AlignToByte();
            return writer.ToArray();
        }
    }
}
