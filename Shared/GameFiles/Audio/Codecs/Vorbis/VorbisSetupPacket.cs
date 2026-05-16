using System.Text;

namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisSetupPacket
    {
        private const byte ExpectedPacketType = 0x05;
        private const string ExpectedHeaderTag = "vorbis";
        private const int HeaderTagByteLength = 6;
        private const int PacketPrefixSize = 1 + HeaderTagByteLength;

        public byte PacketType { get; set; } = ExpectedPacketType;
        public string HeaderTag { get; set; } = ExpectedHeaderTag;
        public byte[] SetupData { get; set; } = [];

        public bool HasData => !string.IsNullOrEmpty(HeaderTag) && SetupData.Length > 0;

        public static VorbisSetupPacket ReadData(byte[] packetData)
        {
            if (packetData.Length < PacketPrefixSize)
                throw new InvalidDataException("Vorbis setup packet is too short.");

            var packet = new VorbisSetupPacket
            {
                PacketType = packetData[0],
                HeaderTag = Encoding.ASCII.GetString(packetData, 1, HeaderTagByteLength),
                SetupData = packetData[PacketPrefixSize..],
            };

            if (packet.PacketType != ExpectedPacketType)
                throw new InvalidDataException($"Vorbis setup header packet type must be 0x{ExpectedPacketType:X2}.");
            if (packet.HeaderTag != ExpectedHeaderTag)
                throw new InvalidDataException("Vorbis setup header is missing the expected tag.");

            return packet;
        }

        public byte[] WriteData()
        {
            var data = new byte[PacketPrefixSize + SetupData.Length];
            data[0] = PacketType;
            Encoding.ASCII.GetBytes(HeaderTag).CopyTo(data, 1);
            SetupData.CopyTo(data, PacketPrefixSize);
            return data;
        }
    }
}
