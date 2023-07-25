using Filetypes.ByteParsing;
using System.Collections.Generic;
using System.Linq;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkEvent_v136 : HircItem, ICAkEvent
    {
        public class Action
        {
            public uint ActionId { get; set; }
        }

        public List<Action> Actions { get; set; } = new List<Action>();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            var actionCount = chunk.ReadByte();
            for (int i = 0; i < actionCount; i++)
                Actions.Add(new Action() { ActionId = chunk.ReadUInt32() });
        }

        public List<uint> GetActionIds() => Actions.Select(x => x.ActionId).ToList();

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)Actions.Count, out _));
            foreach (var action in Actions)
                memStream.Write(ByteParsers.UInt32.EncodeValue(action.ActionId, out _));

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkEvent_v136();
            var chunk = new ByteChunk(byteArray);
            copyInstance.Parse(chunk);

            return byteArray;
        }

        public override void UpdateSize()
        {
            Size = (uint)(HircHeaderSize + 1 + 4 * Actions.Count);
        }


    }
}
