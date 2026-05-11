using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class WemFile
    {
        public WemFileHeader Header { get; set; } = new WemFileHeader(0);
        public FmtChunk FmtChunk { get; set; } = new FmtChunk();
        public DataChunk DataChunk { get; set; } = new DataChunk();
        public JunkChunk? JunkChunk { get; set; }
        public AkdChunk? AkdChunk { get; set; }

        public int DataAlignmentBytes { get; set; }
        public CueChunk? CueChunk { get; set; }
        public List<UnknownChunk> UnknownChunks { get; set; } = [];

        public static WemFile FromBytes(byte[] wemData)
        {
            ArgumentNullException.ThrowIfNull(wemData);
            var wemFile = new WemFile();
            wemFile.ReadData(new ByteChunk(wemData));
            return wemFile;
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

                if (tag == WemChunks.Fmt)
                {
                    if (riffChunk is FmtChunk fmt)
                    {
                        FmtChunk = fmt;
                        hasFmt = true;
                    }
                }
                else if (tag == WemChunks.Data)
                {
                    if (!hasFmt)
                        throw new InvalidDataException("WEM data chunk must appear after the fmt chunk.");

                    if (riffChunk is DataChunk data)
                    {
                        DataChunk = data;
                        hasData = true;
                    }
                }
                else if (tag == WemChunks.Junk)
                    JunkChunk = riffChunk as JunkChunk;
                else if (tag == WemChunks.Akd)
                    AkdChunk = riffChunk as AkdChunk;
                else if (tag == WemChunks.Cue)
                    CueChunk = riffChunk as CueChunk;
                else
                {
                    if (riffChunk is UnknownChunk unknown)
                        UnknownChunks.Add(unknown);
                }

                // Skip padding
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
                var akdDataSize = AkdChunk.WriteData().Length;
                byte[] junkData;

                // When DataAlignmentBytes is set we compute JUNK payload so data starts at that absolute byte offset
                if (DataAlignmentBytes > 0)
                {
                    var chunkHeaderSize = (int)RiffChunkHeader.HeaderSize;

                    var beforeJunk = WemFileHeader.Size + chunkHeaderSize + fmtData.Length + chunkHeaderSize;
                    var afterJunk = chunkHeaderSize + akdDataSize + chunkHeaderSize;
                    var junkSize = DataAlignmentBytes - beforeJunk - afterJunk;
                    junkData = junkSize > 0 ? new byte[junkSize] : [];
                }
                else
                    junkData = JunkChunk?.PaddingBytes ?? [];

                var junkChunkHeader = new RiffChunkHeader(WemChunks.Junk, (uint)junkData.Length);
                var junkChunkHeaderData = RiffChunkHeader.WriteData(junkChunkHeader);
                stream.Write(junkChunkHeaderData);
                stream.Write(junkData);
                if (junkData.Length % RiffChunkHeader.ChunkPaddingAlignment != 0)
                    RiffChunk.WritePadding(stream);

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
