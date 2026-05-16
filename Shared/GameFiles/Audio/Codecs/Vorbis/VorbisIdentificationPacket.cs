using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisIdentificationPacket
    {
        private const byte ExpectedPacketType = 0x01;
        private const string ExpectedHeaderTag = "vorbis";

        public byte PacketType { get; set; } = ExpectedPacketType;
        public string HeaderTag { get; set; } = ExpectedHeaderTag;
        public uint Version { get; set; }
        public byte Channels { get; set; }
        public uint SampleRate { get; set; }
        public uint BitrateMaximum { get; set; }
        public uint NominalBitrate { get; set; }
        public uint BitrateMinimum { get; set; }
        public byte SmallBlockSizeExponent { get; set; }
        public byte LargeBlockSizeExponent { get; set; }
        public uint FramingBit { get; set; } = 1u;

        public bool HasData => !string.IsNullOrEmpty(HeaderTag);

        public static VorbisIdentificationPacket ReadData(byte[] packetData)
        {
            var reader = new ByteChunk(packetData);
            var packet = new VorbisIdentificationPacket
            {
                PacketType = reader.ReadByte(),
                HeaderTag = Encoding.ASCII.GetString(reader.ReadBytes(6)),
            };

            if (packet.PacketType != ExpectedPacketType)
                throw new InvalidDataException($"Vorbis identification header packet type must be 0x{ExpectedPacketType:X2}.");
            if (packet.HeaderTag != ExpectedHeaderTag)
                throw new InvalidDataException("Vorbis identification header is missing the expected tag.");

            packet.Version = reader.ReadUInt32();
            packet.Channels = reader.ReadByte();
            packet.SampleRate = reader.ReadUInt32();
            packet.BitrateMaximum = reader.ReadUInt32();
            packet.NominalBitrate = reader.ReadUInt32();
            packet.BitrateMinimum = reader.ReadUInt32();

            var blockSizeByte = reader.ReadByte();
            packet.SmallBlockSizeExponent = (byte)BitHelper.ExtractBits(blockSizeByte, 0, 4);
            packet.LargeBlockSizeExponent = (byte)BitHelper.ExtractBits(blockSizeByte, 4, 4);

            var framingByte = reader.ReadByte();
            packet.FramingBit = (uint)BitHelper.ExtractBits(framingByte, 0, 1);
            if (packet.FramingBit != 1u)
                throw new InvalidDataException("Vorbis identification header framing flag must be set.");

            if (reader.BytesLeft != 0)
                throw new InvalidDataException("Vorbis identification packet contains trailing data.");

            return packet;
        }

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.Byte.EncodeValue(PacketType, out _));
            stream.Write(Encoding.ASCII.GetBytes(HeaderTag));
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
