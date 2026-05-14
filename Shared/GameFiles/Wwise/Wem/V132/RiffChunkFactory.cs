namespace Shared.GameFormats.Wwise.Wem.V132
{

    public static class RiffChunkFactory
    {
        private static readonly Dictionary<string, Func<RiffChunk>> s_chunkFactories = BuildChunkFactories();

        private static Dictionary<string, Func<RiffChunk>> BuildChunkFactories()
        {
            return new Dictionary<string, Func<RiffChunk>>
            {
                [FmtChunk.ChunkTag] = static () => new FmtChunk(),
                [DataChunk.ChunkTag] = static () => new DataChunk(),
                [JunkChunk.ChunkTag] = static () => new JunkChunk(),
                [CueChunk.ChunkTag] = static () => new CueChunk(),
                [AkdChunk.ChunkTag] = static () => new AkdChunk(),
            };
        }

        public static RiffChunk CreateChunk(string tag)
        {
            if (s_chunkFactories.TryGetValue(tag, out var factory))
                return factory();

            return new UnknownChunk { Tag = tag };
        }
    }
}
