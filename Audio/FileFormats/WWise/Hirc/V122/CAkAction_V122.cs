using Filetypes.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc.V122
{
    public class CAkAction_V122 : HircItem, ICAkAction
    {
        public ActionType ActionType { get; set; }
        public uint SoundId { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            SoundId = chunk.ReadUInt32();
        }

        public ActionType GetActionType() => ActionType;
        public uint GetChildId() => SoundId;

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
