using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V122
{
    public class CAkAction_V122 : CAkAction
    {
        public ActionType ActionType { get; set; }
        public uint SoundId { get; set; }

        protected override void Create(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            SoundId = chunk.ReadUInt32();
        }

        public override ActionType GetActionType() => ActionType;
        public override uint GetSoundId() => SoundId;
    }
}
