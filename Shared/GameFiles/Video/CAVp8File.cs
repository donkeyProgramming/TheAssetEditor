using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Video
{
    public class CAVp8ExtraHeaderData
    {
        public byte UnknownByte { get; set; }
        public uint UnknownUInt32First { get; set; }
        public uint UnknownUInt32Second { get; set; }
    }

    public class CAVp8File
    {
        private const string Signature = "CAMV";
        private const ushort HeaderLengthV0 = 40;
        private const ushort HeaderLengthV1 = 40;

        public ushort Version { get; set; }
        public string CodecFourCC { get; set; } = "VP80";
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public uint NumberOfFrames { get; set; }
        public float Framerate { get; set; }
        public CAVp8ExtraHeaderData? ExtraData { get; set; }
        public List<FrameTableRecord> FrameTable { get; set; } = [];
        public byte[] FrameData { get; set; } = [];

        public CAVp8File(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            ReadData(new ByteChunk(data));
        }

        private void ReadData(ByteChunk chunk)
        {
            var bytes = chunk.ReadBytes(chunk.BytesLeft);
            using var reader = new BinaryReader(new MemoryStream(bytes));

            var signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (signature != Signature)
                throw new Exception($"CA_VP8 signature mismatch: expected '{Signature}' but got '{signature}'.");

            Version = reader.ReadUInt16();
            var rawHeaderLength = reader.ReadUInt16();
            CodecFourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            var msPerFrame = reader.ReadSingle();
            reader.ReadUInt32(); // No idea, always 1.
            var numFramesMinusOne = reader.ReadUInt32(); // Same as num_frames, but sometimes is num_frames - 1. When it's the same, there are 9 extra bytes in the header.
            var offsetFrameTable = reader.ReadUInt32();
            NumberOfFrames = reader.ReadUInt32();
            reader.ReadUInt32(); // Largest frame's size, in bytes. Recalculated on save.

            if (numFramesMinusOne == NumberOfFrames)
            {
                ExtraData = new CAVp8ExtraHeaderData
                {
                    UnknownByte = reader.ReadByte(),
                    UnknownUInt32First = reader.ReadUInt32(),
                    UnknownUInt32Second = reader.ReadUInt32(),
                };
            }

            var expectedHeaderEnd = (long)(rawHeaderLength + 8);
            if (reader.BaseStream.Position != expectedHeaderEnd)
                throw new Exception($"CA_VP8 header size mismatch: expected stream to be at position {expectedHeaderEnd} after reading the header, but it is at position {reader.BaseStream.Position}.");

            var frameDataLength = (int)(offsetFrameTable - reader.BaseStream.Position);
            FrameData = reader.ReadBytes(frameDataLength);

            var totalFileLength = reader.BaseStream.Length;
            var frameTableLength = totalFileLength - reader.BaseStream.Position;
            var hasBells = frameTableLength / 13 == NumberOfFrames && frameTableLength % 13 == 0;

            var runningOffset = 0u;
            FrameTable = new List<FrameTableRecord>((int)NumberOfFrames);
            using var frameTableDecoded = new MemoryStream();

            for (var frameIndex = 0; frameIndex < NumberOfFrames; frameIndex++)
            {
                var frameOffsetReal = reader.ReadUInt32();
                var frameSize = reader.ReadUInt32();
                if (hasBells)
                    reader.ReadUInt32();
                var isKeyFrame = reader.ReadBoolean();

                var frame = new FrameTableRecord
                {
                    Offset = runningOffset,
                    Size = frameSize,
                    IsKeyFrame = isKeyFrame,
                };

                runningOffset += frame.Size;
                FrameTable.Add(frame);

                var frameOffsetRealEnd = frameOffsetReal + frameSize;
                if (frameOffsetRealEnd > totalFileLength)
                    throw new Exception($"CA_VP8 frame {frameIndex} has an incorrect or unknown frame size: it would end at file offset {frameOffsetRealEnd} which is beyond the end of the file at {totalFileLength}.");

                var savedPosition = reader.BaseStream.Position;
                reader.BaseStream.Seek(frameOffsetReal, SeekOrigin.Begin);
                frameTableDecoded.Write(reader.ReadBytes((int)frameSize));
                reader.BaseStream.Seek(savedPosition, SeekOrigin.Begin);
            }

            if (reader.BaseStream.Position != totalFileLength)
                throw new Exception($"CA_VP8 file size mismatch: expected {totalFileLength} bytes but stream is at position {reader.BaseStream.Position}.");

            Framerate = 1000f / msPerFrame;
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            ushort headerLength;
            if (Version == 0)
                headerLength = ExtraData != null ? (ushort)(HeaderLengthV0 + 9) : HeaderLengthV0;
            else
                headerLength = ExtraData != null ? (ushort)(HeaderLengthV1 + 9) : HeaderLengthV1;

            var rawHeaderLength = (ushort)(headerLength - 8);

            writer.Write(Encoding.ASCII.GetBytes(Signature));
            writer.Write(Version);
            writer.Write(rawHeaderLength);
            writer.Write(Encoding.ASCII.GetBytes(CodecFourCC));
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(1000f / Framerate);
            writer.Write((uint)1);

            if (ExtraData != null || NumberOfFrames == 0)
                writer.Write(NumberOfFrames);
            else
                writer.Write(NumberOfFrames - 1);

            writer.Write((uint)(headerLength + FrameData.Length));
            writer.Write(NumberOfFrames);
            writer.Write(FrameTable.Max(frame => frame.Size));

            if (ExtraData != null)
            {
                writer.Write(ExtraData.UnknownByte);
                writer.Write(ExtraData.UnknownUInt32First);
                writer.Write(ExtraData.UnknownUInt32Second);
            }

            writer.Write(FrameData);

            var runningOffset = ExtraData != null ? (uint)rawHeaderLength : (uint)headerLength;
            foreach (var frame in FrameTable)
            {
                writer.Write(runningOffset);
                writer.Write(frame.Size);
                writer.Write(frame.IsKeyFrame);
                runningOffset += frame.Size;
            }

            return memStream.ToArray();
        }
    }
}
