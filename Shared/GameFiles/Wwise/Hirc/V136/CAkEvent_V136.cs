using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkEvent_V136 : HircItem, ICAkEvent
    {
        public uint ActionListSize { get; set; }
        public List<Action_V136> Actions { get; set; } = [];

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            ActionListSize = chunk.ReadByte();
            for (var i = 0; i < ActionListSize; i++)
                Actions.Add(Action_V136.Create(chunk));
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)Actions.Count, out _));
            foreach (var action in Actions)
                memStream.Write(action.GetAsByteArray());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkEvent_V136();
            var chunk = new ByteChunk(byteArray);
            sanityReload.Parse(chunk);

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            var actionListSizeSize = ByteHelper.GetPropertyTypeSize(ActionListSize);

            uint actionListSize = 0;
            foreach (var action in Actions)
                actionListSize += action.GetSize();

            SectionSize = idSize + actionListSizeSize + actionListSize;
        }

        public List<uint> GetActionIds() => Actions.Select(x => x.ActionId).ToList();

        public class Action_V136
        {
            public uint ActionId { get; set; }
            
            public static Action_V136 Create(ByteChunk chunk)
            {
                return new Action_V136()
                {
                    ActionId = chunk.ReadUInt32()
                };
            }

            public byte[] GetAsByteArray()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(ActionId, out _));
                return memStream.ToArray();
            }

            public uint GetSize()
            {
                var actionIdSize = ByteHelper.GetPropertyTypeSize(ActionId);
                return actionIdSize;
            }
        }
    }
}
