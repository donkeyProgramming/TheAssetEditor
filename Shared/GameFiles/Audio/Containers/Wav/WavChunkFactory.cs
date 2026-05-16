namespace Shared.GameFormats.Audio.Containers.Wav
{
    public static class WavChunkFactory
    {
        public static RiffChunk? CreateChunk(string tag)
        {
            return tag switch
            {
                FmtChunk.ChunkTag => new FmtChunk(),
                DataChunk.ChunkTag => new DataChunk(),
                _ => null,
            };
        }
    }
}
