using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Didx
{
    public partial class DidxChunk
    {
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public List<MediaHeader> MediaList { get; set; } = [];

        public static DidxChunk ReadData(string fileName, ByteChunk chunk)
        {
            var didxChunk = new DidxChunk { ChunkHeader = ChunkHeader.ReadData(chunk) };
            didxChunk.MediaList = ReadMediaHeaders(chunk, didxChunk.ChunkHeader.ChunkSize);
            return didxChunk;
        }

        public static List<MediaHeader> ReadMediaHeaders(ByteChunk chunk, uint chunkSize)
        {
            if (chunkSize % MediaHeader.ByteSize != 0)
                throw new InvalidDataException($"DIDX chunk size {chunkSize} is not a multiple of {MediaHeader.ByteSize}.");

            var items = chunkSize / MediaHeader.ByteSize;
            var mediaHeaders = new List<MediaHeader>((int)items);
            for (var itemIndex = 0; itemIndex < items; itemIndex++)
                mediaHeaders.Add(MediaHeader.ReadData(chunk));

            return mediaHeaders;
        }
    }
}
