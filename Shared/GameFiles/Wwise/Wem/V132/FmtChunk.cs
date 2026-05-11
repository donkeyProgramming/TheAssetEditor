using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class FmtChunk : RiffChunk
    {
        private const ushort WwiseVorbisFormatTag = 0xFFFF;
        private const int WwiseFmtExtraDataSize = 0x30;
        private const uint WwiseReservedFieldValue = 0x00008000u;
        private const uint ChunkByteSize = 66;

        // Standard WAVEFORMATEX
        public ushort FormatTag { get; set; }
        public ushort Channels { get; set; }
        public uint SampleRate { get; set; }
        public uint AverageBytesPerSecond { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }
        public ushort ExtraDataSize { get; set; }
        public ushort VorbisExtra { get; set; }

        // Wwise Vorbis extension
        public uint ChannelMask { get; set; }
        public int SampleCount { get; set; }
        public uint SetupPacketSize { get; set; }
        public uint AudioDataOffset { get; set; }
        public ushort Reserved0 { get; set; }
        public ushort LastGranuleExtra { get; set; }
        public uint SeekTableSize { get; set; }
        public uint FirstAudioPacketOffset { get; set; }
        public ushort MaxPacketSize { get; set; }
        public ushort LastGranuleExtra2 { get; set; }
        public uint Reserved1 { get; set; }
        public uint Reserved2 { get; set; }
        public uint CodebookHash { get; set; }
        public byte SmallBlockSizeExponent { get; set; }
        public byte LargeBlockSizeExponent { get; set; }

        public FmtChunk()
        {
            Tag = WemChunks.Fmt;
            FormatTag = WwiseVorbisFormatTag;
            BlockAlign = 0;
            BitsPerSample = 0;
            ExtraDataSize = WwiseFmtExtraDataSize;
            VorbisExtra = 0;
            Reserved0 = 0;
            Reserved1 = WwiseReservedFieldValue;
            Reserved2 = WwiseReservedFieldValue;
        }

        public override void ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < ChunkByteSize)
                throw new InvalidDataException($"WEM fmt chunk must be at least {ChunkByteSize} bytes.");

            FormatTag = chunk.ReadUShort();
            if (FormatTag != WwiseVorbisFormatTag)
                throw new InvalidDataException($"WEM format tag 0x{FormatTag:X4} is not supported.");

            Channels = chunk.ReadUShort();
            SampleRate = chunk.ReadUInt32();
            AverageBytesPerSecond = chunk.ReadUInt32();
            BlockAlign = chunk.ReadUShort();
            BitsPerSample = chunk.ReadUShort();
            ExtraDataSize = chunk.ReadUShort();

            if (ExtraDataSize != WwiseFmtExtraDataSize)
                throw new InvalidDataException($"WEM Vorbis extra data size 0x{ExtraDataSize:X} is not supported. Expected 0x{WwiseFmtExtraDataSize:X}.");

            VorbisExtra = chunk.ReadUShort();
            ChannelMask = chunk.ReadUInt32();

            var v132Marker = BitHelper.ExtractBits(ChannelMask, 8, 4);
            if (v132Marker == 0)
                throw new InvalidDataException("Only Wwise Vorbis V132 is supported. The V132 marker in the fmt chunk is not present.");

            SampleCount = chunk.ReadInt32();
            SetupPacketSize = chunk.ReadUInt32();
            AudioDataOffset = chunk.ReadUInt32();
            Reserved0 = chunk.ReadUShort();
            LastGranuleExtra = chunk.ReadUShort();
            SeekTableSize = chunk.ReadUInt32();
            FirstAudioPacketOffset = chunk.ReadUInt32();
            MaxPacketSize = chunk.ReadUShort();
            LastGranuleExtra2 = chunk.ReadUShort();
            Reserved1 = chunk.ReadUInt32();
            Reserved2 = chunk.ReadUInt32();
            CodebookHash = chunk.ReadUInt32();
            SmallBlockSizeExponent = chunk.ReadByte();
            LargeBlockSizeExponent = chunk.ReadByte();
        }

        public override byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.UShort.EncodeValue(FormatTag, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(Channels, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SampleRate, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(AverageBytesPerSecond, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(BlockAlign, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(BitsPerSample, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(ExtraDataSize, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(VorbisExtra, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(ChannelMask, out _));
            stream.Write(ByteParsers.Int32.EncodeValue(SampleCount, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SetupPacketSize, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(AudioDataOffset, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(Reserved0, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(LastGranuleExtra, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SeekTableSize, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(FirstAudioPacketOffset, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(MaxPacketSize, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(LastGranuleExtra2, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(Reserved1, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(Reserved2, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(CodebookHash, out _));
            stream.WriteByte(SmallBlockSizeExponent);
            stream.WriteByte(LargeBlockSizeExponent);

            var byteArray = stream.ToArray();
            var sanityReload = new FmtChunk();
            sanityReload.ReadChunk(new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
