using Shared.ByteParsing;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class WemFile
    {
        public WemFileHeader Header { get; set; } = new WemFileHeader(0);
        public FmtChunk FmtChunk { get; set; } = new FmtChunk();
        public DataChunk DataChunk { get; set; } = new DataChunk();
        public JunkChunk? JunkChunk { get; set; }
        public AkdChunk? AkdChunk { get; set; }
        public CueChunk? CueChunk { get; set; }
        public List<UnknownChunk> UnknownChunks { get; set; } = [];

        public static WemFile CreateFromBytes(byte[] wemData)
        {
            ArgumentNullException.ThrowIfNull(wemData);
            var wemFile = new WemFile();
            wemFile.ReadData(new ByteChunk(wemData));
            return wemFile;
        }

        public static WemFile CreateFromWavBytes(byte[] wavData, WemEncodingSettings? encodingSettings = null)
        {
            ArgumentNullException.ThrowIfNull(wavData);
            var codebookLibrary = new WwiseCodebookLibrary();
            var encoder = new WemVorbisEncoder(codebookLibrary);
            if (encodingSettings != null)
                encoder.EncodingSettings = encodingSettings;

            return encoder.EncodeFromWav(wavData);
        }

        public void ReadData(ByteChunk chunk)
        {
            Header = WemFileHeader.ReadData(chunk);

            var hasFmt = false;
            var hasData = false;

            while (chunk.BytesLeft >= RiffChunkHeader.HeaderSize)
            {
                var chunkStartIndex = chunk.Index;
                var chunkHeader = RiffChunkHeader.ReadData(chunk);
                var tag = chunkHeader.Tag;
                var size = chunkHeader.ChunkSize;

                if (size > (uint)chunk.BytesLeft)
                    throw new InvalidDataException($"Chunk {tag} claims size {size} but only {chunk.BytesLeft} bytes remaining.");

                var chunkData = chunk.ReadBytes((int)size);
                var byteChunk = new ByteChunk(chunkData);

                var riffChunk = RiffChunkFactory.CreateChunk(tag);
                if (riffChunk is not UnknownChunk && riffChunk.Tag != tag)
                    throw new InvalidDataException($"Chunk factory returned '{riffChunk.Tag}' for file chunk '{tag}'.");

                riffChunk.ByteIndexInFile = (uint)chunkStartIndex;

                if (riffChunk is DataChunk dataChunk)
                {
                    if (!hasFmt)
                        throw new InvalidDataException("WEM data chunk must appear after the fmt chunk.");

                    dataChunk.FmtChunk = FmtChunk;
                }

                riffChunk.ReadChunk(byteChunk);

                if (tag == FmtChunk.ChunkTag)
                {
                    if (riffChunk is FmtChunk fmt)
                    {
                        FmtChunk = fmt;
                        hasFmt = true;
                    }
                }
                else if (tag == DataChunk.ChunkTag)
                {
                    if (!hasFmt)
                        throw new InvalidDataException("WEM data chunk must appear after the fmt chunk.");

                    if (riffChunk is DataChunk data)
                    {
                        DataChunk = data;
                        hasData = true;
                    }
                }
                else if (tag == JunkChunk.ChunkTag)
                    JunkChunk = riffChunk as JunkChunk;
                else if (tag == AkdChunk.ChunkTag)
                    AkdChunk = riffChunk as AkdChunk;
                else if (tag == CueChunk.ChunkTag)
                    CueChunk = riffChunk as CueChunk;
                else
                {
                    if (riffChunk is UnknownChunk unknown)
                        UnknownChunks.Add(unknown);
                }

                // Padding
                if (size % RiffChunkHeader.ChunkPaddingAlignment != 0)
                    chunk.Advance(1);
            }

            if (!hasFmt || !hasData)
                throw new InvalidDataException("WEM file must contain both a fmt chunk and a data chunk.");
        }

        public byte[] WriteData()
        {
            if (CueChunk != null)
                throw new NotSupportedException("Writing WEM files with a cue chunk is not supported.");
            if (UnknownChunks.Count > 0)
                throw new NotSupportedException($"Writing WEM files with unknown chunks is not supported: {string.Join(", ", UnknownChunks.Select(c => c.Tag))}.");

            using var stream = new MemoryStream();

            var riffHeaderPosition = stream.Position;

            Header.RiffSize = 0u;
            WemFileHeader.WriteData(stream, Header);

            var fmtData = FmtChunk.WriteData();
            var fmtChunkHeader = new RiffChunkHeader(FmtChunk.Tag, (uint)fmtData.Length);
            var fmtChunkHeaderData = RiffChunkHeader.WriteData(fmtChunkHeader);
            stream.Write(fmtChunkHeaderData);
            stream.Write(fmtData);
            if (fmtData.Length % RiffChunkHeader.ChunkPaddingAlignment != 0)
                RiffChunk.WritePadding(stream);

            if (AkdChunk != null)
            {
                if (JunkChunk != null)
                {
                    var junkData = JunkChunk.WriteData();

                    var junkChunkHeader = new RiffChunkHeader(JunkChunk.ChunkTag, (uint)junkData.Length);
                    var junkChunkHeaderData = RiffChunkHeader.WriteData(junkChunkHeader);
                    stream.Write(junkChunkHeaderData);
                    stream.Write(junkData);
                    if (junkData.Length % RiffChunkHeader.ChunkPaddingAlignment != 0)
                        RiffChunk.WritePadding(stream);
                }

                var akdData = AkdChunk.WriteData();
                var akdChunkHeader = new RiffChunkHeader(AkdChunk.Tag, (uint)akdData.Length);
                var akdChunkHeaderData = RiffChunkHeader.WriteData(akdChunkHeader);
                stream.Write(akdChunkHeaderData);
                stream.Write(akdData);
                if (akdData.Length % RiffChunkHeader.ChunkPaddingAlignment != 0)
                    RiffChunk.WritePadding(stream);
            }

            var dataData = DataChunk.WriteData();
            var dataChunkHeader = new RiffChunkHeader(DataChunk.Tag, (uint)dataData.Length);
            var dataChunkHeaderData = RiffChunkHeader.WriteData(dataChunkHeader);
            stream.Write(dataChunkHeaderData);
            stream.Write(dataData);
            if (dataData.Length % RiffChunkHeader.ChunkPaddingAlignment != 0)
                RiffChunk.WritePadding(stream);

            // Update RIFF size
            var totalSize = (uint)(stream.Length - WemFileHeader.BytesBeforeSize);
            Header.RiffSize = totalSize;
            stream.Position = riffHeaderPosition + 4;
            stream.Write(BitConverter.GetBytes(totalSize));

            return stream.ToArray();
        }
    }
}
