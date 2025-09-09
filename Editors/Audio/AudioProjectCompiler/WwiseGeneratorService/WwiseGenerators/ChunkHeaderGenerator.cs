using Shared.GameFormats.Wwise;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators
{
    public class ChunkHeaderGenerator
    {
        public static ChunkHeader GenerateChunkHeader(string tag, uint chunkSize)
        {
            return new ChunkHeader
            {
                Tag = tag,
                ChunkSize = chunkSize
            };
        }
    }
}
