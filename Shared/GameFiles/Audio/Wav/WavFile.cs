using Shared.ByteParsing;
using Shared.GameFormats.Audio.Codecs;

namespace Shared.GameFormats.Audio.Wav
{
    public class WavFile
    {
        private const int StandardPcmWavHeaderSize = 44;

        public WavFileHeader Header { get; set; } = new();
        public FmtChunk FmtChunk { get; set; } = new();
        public DataChunk DataChunk { get; set; } = new();
        public PcmAudio Audio { get; set; } = new();

        public static WavFile CreateFromBytes(byte[] wavData)
        {
            ArgumentNullException.ThrowIfNull(wavData);
            var wavFile = new WavFile();
            wavFile.ReadData(new ByteChunk(wavData));
            return wavFile;
        }

        public void ReadData(ByteChunk chunk)
        {
            Header = WavFileHeader.ReadData(chunk);

            FmtChunk? fmtChunk = null;
            DataChunk? dataChunk = null;

            while (chunk.BytesLeft >= WavChunkHeader.HeaderSize)
            {
                var chunkStartIndex = chunk.Index;
                var chunkHeader = WavChunkHeader.ReadData(chunk);
                var chunkData = new ByteChunk(chunk.ReadBytes(chunkHeader.ChunkSize));

                var wavChunk = WavChunkFactory.CreateChunk(chunkHeader.Tag);
                if (wavChunk != null)
                {
                    wavChunk.ByteIndexInFile = (uint)chunkStartIndex;
                    wavChunk.ReadChunk(chunkData);

                    if (chunkHeader.Tag == FmtChunk.ChunkTag)
                    {
                        if (wavChunk is FmtChunk fmt)
                            fmtChunk = fmt;
                    }
                    else if (chunkHeader.Tag == DataChunk.ChunkTag)
                    {
                        if (wavChunk is DataChunk data)
                            dataChunk = data;
                    }
                }

                // Padding
                if (chunkHeader.ChunkSize % WavChunkHeader.ChunkPaddingAlignment != 0 && chunk.BytesLeft > 0)
                    chunk.Advance(1);
            }

            if (fmtChunk == null || dataChunk == null)
                throw new InvalidDataException("WAV file is missing required fmt or data chunk.");

            if (fmtChunk.FormatTag == 0 || fmtChunk.Channels <= 0 || fmtChunk.SampleRate <= 0 || 
                fmtChunk.BitsPerSample <= 0 || dataChunk.Data.Length == 0)
                throw new InvalidDataException("WAV file has invalid fmt or data chunk information.");

            FmtChunk = fmtChunk;
            DataChunk = dataChunk;
            Audio = new PcmAudio
            {
                BitsPerSample = fmtChunk.BitsPerSample,
                Channels = fmtChunk.Channels,
                Data = dataChunk.Data,
                SampleRate = fmtChunk.SampleRate,
            };
        }

        public byte[] WriteData()
        {
            ArgumentNullException.ThrowIfNull(Audio);

            FmtChunk.BitsPerSample = Audio.BitsPerSample;
            FmtChunk.Channels = Audio.Channels;
            FmtChunk.SampleRate = Audio.SampleRate;
            FmtChunk.FormatTag = FmtChunk.PcmFormatTag;
            DataChunk.Data = Audio.Data;

            var bitsPerSample = Audio.BitsPerSample;
            var blockAlign = checked((ushort)(Audio.Channels * (bitsPerSample / BitHelper.BitsPerByte)));
            var byteRate = checked(Audio.SampleRate * blockAlign);
            var bytesBeforeSize = WavFileHeader.BytesBeforeSize;
            var riffChunkSize = StandardPcmWavHeaderSize - bytesBeforeSize + DataChunk.Data.Length;

            Header.ContainerId = WavFileHeader.RiffContainerId;
            Header.FormType = WavFileHeader.WaveFormType;
            Header.RiffChunkSize = riffChunkSize;

            FmtChunk.Size = FmtChunk.ChunkSize;
            FmtChunk.ByteRate = byteRate;
            FmtChunk.BlockAlign = blockAlign;

            var fmtData = FmtChunk.WriteData();
            var dataData = DataChunk.WriteData();

            using var stream = new MemoryStream(StandardPcmWavHeaderSize + dataData.Length);

            WavFileHeader.WriteData(stream, Header);

            var fmtChunkHeader = new WavChunkHeader { Tag = FmtChunk.ChunkTag, ChunkSize = fmtData.Length };
            WavChunkHeader.WriteData(stream, fmtChunkHeader);
            stream.Write(fmtData);

            var dataChunkHeader = new WavChunkHeader { Tag = DataChunk.ChunkTag, ChunkSize = dataData.Length };
            WavChunkHeader.WriteData(stream, dataChunkHeader);
            stream.Write(dataData);

            return stream.ToArray();
        }
    }
}
