using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    public class CAkEvent_v136 : CAkEvent
    {
        public class Action
        {
            public uint ActionId { get; set; }
        }

        public List<Action> Actions { get; set; } = new List<Action>();

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            var actionCount = chunk.ReadByte();
            for (int i = 0; i < actionCount; i++)
                Actions.Add(new Action() { ActionId = chunk.ReadUInt32() });
        }

        public override List<uint> GetActionIds()
        {
            return Actions.Select(x => x.ActionId).ToList();
        }
    }
}
