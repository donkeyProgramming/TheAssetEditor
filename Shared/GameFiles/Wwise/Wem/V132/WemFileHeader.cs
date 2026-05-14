using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class WemFileHeader(uint riffSize)
    {
        public const int Size = 12;
        public const int BytesBeforeSize = 8;
        public const string RiffContainerId = "RIFF";
        public const string WaveFormType = "WAVE";

        public string ContainerId { get; set; } = RiffContainerId;
        public string FormType { get; set; } = WaveFormType;
        public uint RiffSize { get; set; } = riffSize;

        public static WemFileHeader ReadData(ByteChunk chunk)
        {
            var containerId = System.Text.Encoding.ASCII.GetString(chunk.ReadBytes(4));
            if (containerId != RiffContainerId)
                throw new InvalidDataException($"Expected RIFF container ID '{RiffContainerId}', got '{containerId}'.");

            var riffSize = chunk.ReadUInt32();

            var formType = System.Text.Encoding.ASCII.GetString(chunk.ReadBytes(4));
            if (formType != WaveFormType)
                throw new InvalidDataException($"Expected WAVE form type '{WaveFormType}', got '{formType}'.");

            var header = new WemFileHeader(riffSize);

            return header;
        }

        public static void WriteData(Stream stream, WemFileHeader header)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(header);

            if (header.ContainerId.Length != 4)
                throw new InvalidDataException($"RIFF container id must be 4 bytes, got '{header.ContainerId}'.");

            if (header.FormType.Length != 4)
                throw new InvalidDataException($"RIFF form type must be 4 bytes, got '{header.FormType}'.");

            stream.Write(System.Text.Encoding.ASCII.GetBytes(header.ContainerId));
            stream.Write(BitConverter.GetBytes(header.RiffSize));
            stream.Write(System.Text.Encoding.ASCII.GetBytes(header.FormType));
        }
    }
}
