using Filetypes.ByteParsing;
using System.Collections.Generic;

namespace FileTypes.Sound.WWise.Hirc
{
    public class CAkEvent : HricItem
    {

        public class Action
        {
            public uint ActionId { get; set; }
        }


        public List<Action> Actions { get; set; } = new List<Action>();

        public static CAkEvent Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var akEvent = new CAkEvent();
            akEvent.LoadCommon(chunk);

            var actionCount = chunk.ReadUInt32();
            for (int i = 0; i < actionCount; i++)
                akEvent.Actions.Add(new Action() { ActionId = chunk.ReadUInt32() });

            akEvent.SkipToEnd(chunk, objectStartIndex + 5);
            return akEvent;

        }
    }
}
