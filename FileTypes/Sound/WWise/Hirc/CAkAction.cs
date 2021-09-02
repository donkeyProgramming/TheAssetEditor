using Filetypes.ByteParsing;

namespace FileTypes.Sound.WWise.Hirc
{
    public class CAkAction : HricItem
    {
        public ActionType ActionType { get; set; }
        public uint SoundId { get; set; }

        public static CAkAction Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var akAction = new CAkAction();
            akAction.LoadCommon(chunk);

            akAction.ActionType = (ActionType)chunk.ReadUShort();
            akAction.SoundId = chunk.ReadUInt32();

            akAction.SkipToEnd(chunk, objectStartIndex + 5);
            return akAction;
        }
    }
}
