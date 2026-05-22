using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisCommentPacket
    {
        private const byte ExpectedPacketType = 0x03;
        private const string ExpectedHeaderTag = "vorbis";

        public byte PacketType { get; set; } = ExpectedPacketType;
        public string HeaderTag { get; set; } = ExpectedHeaderTag;
        public string VendorString { get; set; } = string.Empty;
        public List<string> UserComments { get; set; } = [];
        public uint FramingBit { get; set; } = 1u;

        public bool HasData => !string.IsNullOrEmpty(HeaderTag);

        public static VorbisCommentPacket ReadData(byte[] packetData)
        {
            var reader = new ByteChunk(packetData);
            var packet = new VorbisCommentPacket
            {
                PacketType = reader.ReadByte(),
                HeaderTag = Encoding.ASCII.GetString(reader.ReadBytes(6)),
            };

            if (packet.PacketType != ExpectedPacketType)
                throw new InvalidDataException($"Vorbis comment header packet type must be 0x{ExpectedPacketType:X2}.");
            if (packet.HeaderTag != ExpectedHeaderTag)
                throw new InvalidDataException("Vorbis comment header is missing the expected tag.");

            var vendorStringLengthU32 = reader.ReadUInt32();
            if (vendorStringLengthU32 > int.MaxValue)
                throw new InvalidDataException("Vorbis comment vendor string length is too large.");

            var vendorStringLength = (int)vendorStringLengthU32;
            if (vendorStringLength > reader.BytesLeft)
                throw new InvalidDataException("Vorbis comment vendor string exceeds packet length.");

            packet.VendorString = Encoding.ASCII.GetString(reader.ReadBytes(vendorStringLength));

            var commentCountU32 = reader.ReadUInt32();
            if (commentCountU32 > int.MaxValue)
                throw new InvalidDataException("Vorbis comment count is too large.");

            var commentCount = (int)commentCountU32;
            packet.UserComments = [];
            for (var commentIndex = 0; commentIndex < commentCount; commentIndex++)
            {
                var commentLengthU32 = reader.ReadUInt32();
                if (commentLengthU32 > int.MaxValue)
                    throw new InvalidDataException("Vorbis comment entry length is too large.");

                var commentLength = (int)commentLengthU32;
                if (commentLength > reader.BytesLeft)
                    throw new InvalidDataException("Vorbis comment entry exceeds packet length.");

                var comment = Encoding.ASCII.GetString(reader.ReadBytes(commentLength));
                packet.UserComments.Add(comment);
            }

            var framingByte = reader.ReadByte();
            packet.FramingBit = (uint)BitHelper.ExtractBits(framingByte, 0, 1);
            if (packet.FramingBit != 1u)
                throw new InvalidDataException("Vorbis comment header framing flag must be set.");

            if (reader.BytesLeft != 0)
                throw new InvalidDataException("Vorbis comment packet contains trailing data.");

            return packet;
        }

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.Byte.EncodeValue(PacketType, out _));
            stream.Write(Encoding.ASCII.GetBytes(HeaderTag));

            var vendorBytes = Encoding.ASCII.GetBytes(VendorString);
            stream.Write(ByteParsers.UInt32.EncodeValue((uint)vendorBytes.Length, out _));
            stream.Write(vendorBytes);

            stream.Write(ByteParsers.UInt32.EncodeValue((uint)UserComments.Count, out _));
            foreach (var comment in UserComments)
            {
                var commentBytes = Encoding.ASCII.GetBytes(comment);
                stream.Write(ByteParsers.UInt32.EncodeValue((uint)commentBytes.Length, out _));
                stream.Write(commentBytes);
            }

            var framingByte = (byte)BitHelper.ExtractBits(FramingBit, 0, 1);
            stream.Write(ByteParsers.Byte.EncodeValue(framingByte, out _));

            return stream.ToArray();
        }
    }
}
