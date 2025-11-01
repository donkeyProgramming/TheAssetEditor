using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc
{
    public class HircChunkGenerator
    {
        public static HircChunk GenerateHircChunk(List<HircItem> hircItems)
        {
            var chunkSize = HircChunk.ChunkHeaderSize + (uint)(hircItems.Sum(hirc => HircItem.HircHeaderSize + hirc.SectionSize));
            var hircChunk = new HircChunk
            {
                ChunkHeader = ChunkHeaderGenerator.GenerateChunkHeader(BankChunkTypes.HIRC, chunkSize),
                NumHircItems = (uint)hircItems.Count,
                HircItems = hircItems
            };
            return hircChunk;
        }
    }
}
