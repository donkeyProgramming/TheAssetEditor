using Filetypes.ByteParsing;

namespace FileTypes.Sound.WWise.Hirc
{
    public class CAkUnknown : HricItem
    {
        public static CAkUnknown Create(ByteChunk chunk)
        {
            var objectStartIndex = chunk.Index;

            var sound = new CAkUnknown();
            sound.LoadCommon(chunk);

            sound.SkipToEnd(chunk, objectStartIndex + 5);
            return sound;

        }
    }
}
