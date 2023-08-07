using Filetypes.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc.V112
{
    public class CAkAction_v112 : HircItem, ICAkAction
    {
        public ActionType ActionType { get; set; }
        public uint SoundId { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            SoundId = chunk.ReadUInt32();
        }

        public ActionType GetActionType() => ActionType;
        public uint GetChildId() => SoundId;
        public uint GetStateGroupId() => throw new NotImplementedException();

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
