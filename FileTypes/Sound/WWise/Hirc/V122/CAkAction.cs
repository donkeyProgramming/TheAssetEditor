using Filetypes.ByteParsing;

namespace FileTypes.Sound.WWise.Hirc.V122
{
    public class CAkAction : HircItem
    {
        public ActionType ActionType { get; set; }
        public uint SoundId { get; set; }

        protected override void Create(ByteChunk chunk)
        {
            ActionType = (ActionType) chunk.ReadUShort();
            SoundId = chunk.ReadUInt32();
        }
    }
}
