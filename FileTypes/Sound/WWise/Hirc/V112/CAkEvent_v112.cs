using Filetypes.ByteParsing;
using System.Collections.Generic;
using System.Linq;

namespace FileTypes.Sound.WWise.Hirc.V112
{
    public class CAkEvent_v112 : CAkEvent 
    {
        public class Action
        {
            public uint ActionId { get; set; }
        }

        public List<Action> Actions { get; set; } = new List<Action>();

        protected override void Create(ByteChunk chunk)
        {
            var actionCount = chunk.ReadUInt32();
            for (int i = 0; i < actionCount; i++)
                Actions.Add(new Action() { ActionId = chunk.ReadUInt32() });
        }

        public override List<uint> GetActionIds()
        {
            return Actions.Select(x => x.ActionId).ToList();
        }
    }
}
