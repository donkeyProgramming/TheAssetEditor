using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;
using System;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    public class CAkAction_v136 : CAkAction
    {
        public ActionType ActionType { get; set; }
        public uint idExt { get; set; }
        public byte idExt_4 { get; set; }

        //public AkPropBundle AkPropBundle0 { get; set; }
        //public AkPropBundle AkPropBundle1 { get; set; }
        //public AkPlayActionParams AkPlayActionParams { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            idExt = chunk.ReadUInt32();
            idExt_4 = chunk.ReadByte();

            //AkPropBundle0 = AkPropBundle.Create(chunk);
            //AkPropBundle1 = AkPropBundle.Create(chunk);
            //AkPlayActionParams = AkPlayActionParams.Create(chunk);
        }

        public override ActionType GetActionType() => ActionType;
        public override uint GetSoundId() => idExt;
    }

    public class AkPlayActionParams
    {
        public byte byBitVector { get; set; }
        public uint bankId { get; set; }

        public static AkPlayActionParams Create(ByteChunk chunk)
        {
            return new AkPlayActionParams()
            {
                byBitVector = chunk.ReadByte(),
                bankId = chunk.ReadUInt32(),
            };
        }
    }
}