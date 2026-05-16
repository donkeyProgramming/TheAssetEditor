using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Containers.Wav
{
    public class WavFileHeader
    {
        public const int Size = 12;
        public const int BytesBeforeSize = 8;
        public const string RiffContainerId = "RIFF";
        public const string WaveFormType = "WAVE";

        public string ContainerId { get; set; } = RiffContainerId;
        public int RiffChunkSize { get; set; }
        public string FormType { get; set; } = WaveFormType;

        public static WavFileHeader ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < Size)
                throw new InvalidDataException($"WAV file header must be at least {Size} bytes.");

            var containerId = Encoding.ASCII.GetString(chunk.ReadBytes(4));
            if (containerId != RiffContainerId)
                throw new InvalidDataException($"Expected RIFF container ID '{RiffContainerId}', got '{containerId}'.");

            var riffSize = chunk.ReadInt32();

            var formType = Encoding.ASCII.GetString(chunk.ReadBytes(4));
            if (formType != WaveFormType)
                throw new InvalidDataException($"Expected WAVE form type '{WaveFormType}', got '{formType}'.");

            return new WavFileHeader
            {
                ContainerId = containerId,
                RiffChunkSize = riffSize,
                FormType = formType,
            };
        }

        public static void WriteData(Stream stream, WavFileHeader header)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(header);

            if (header.ContainerId.Length != 4)
                throw new InvalidDataException($"RIFF container id must be 4 bytes, got '{header.ContainerId}'.");

            if (header.FormType.Length != 4)
                throw new InvalidDataException($"RIFF form type must be 4 bytes, got '{header.FormType}'.");

            stream.Write(Encoding.ASCII.GetBytes(header.ContainerId));
            stream.Write(ByteParsers.Int32.EncodeValue(header.RiffChunkSize, out _));
            stream.Write(Encoding.ASCII.GetBytes(header.FormType));
        }
    }
}
