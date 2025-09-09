using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkEvent_V112 : HircItem, ICAkEvent
    {
        public uint ActionListSize { get; set; }
        public List<Action_V112> Actions { get; set; } = [];

        protected override void ReadData(ByteChunk chunk)
        {
            ActionListSize = chunk.ReadUInt32();
            for (var i = 0; i < ActionListSize; i++)
                Actions.Add(new Action_V112() { ActionId = chunk.ReadUInt32() });
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.UInt32.EncodeValue(ActionListSize, out _));
            foreach (var action in Actions)
                memStream.Write(action.WriteData());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkEvent_V112();
            var chunk = new ByteChunk(byteArray);
            sanityReload.ReadHirc(chunk);

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

        public class Action_V112
        {
            public uint ActionId { get; set; }

            public static Action_V112 ReadData(ByteChunk chunk)
            {
                return new Action_V112()
                {
                    ActionId = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
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
