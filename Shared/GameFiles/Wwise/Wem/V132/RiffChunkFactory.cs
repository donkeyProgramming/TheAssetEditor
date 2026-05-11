namespace Shared.GameFormats.Wwise.Wem.V132
{

    public static class RiffChunkFactory
    {
        private static readonly Dictionary<string, Func<RiffChunk>> s_chunkFactories = BuildChunkFactories();

        private static Dictionary<string, Func<RiffChunk>> BuildChunkFactories()
        {
            return new Dictionary<string, Func<RiffChunk>>
            {
                [WemChunks.Fmt] = static () => new FmtChunk(),
                [WemChunks.Data] = static () => new DataChunk(),
                [WemChunks.Junk] = static () => new JunkChunk(),
                [WemChunks.Cue] = static () => new CueChunk(),
                [WemChunks.Akd] = static () => new AkdChunk(),
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
