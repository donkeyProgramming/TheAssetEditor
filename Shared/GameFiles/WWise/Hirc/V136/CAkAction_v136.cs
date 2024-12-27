using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkAction_v136 : HircItem, ICAkAction
    {
        public ActionType ActionType { get; set; }
        public uint IdExt { get; set; }
        public byte IdExt4 { get; set; }
        public AkPropBundle AkPropBundle0 { get; set; } = new AkPropBundle();
        public AkPropBundle AkPropBundle1 { get; set; } = new AkPropBundle();
        public AkPlayActionParams AkPlayActionParams { get; set; } = new AkPlayActionParams();
        public AkSetStateParams AkSetStateParams { get; set; } = new AkSetStateParams();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            IdExt = chunk.ReadUInt32();
            IdExt4 = chunk.ReadByte();

            if (ActionType == ActionType.Play)
            {
                AkPropBundle0 = AkPropBundle.Create(chunk);
                AkPropBundle1 = AkPropBundle.Create(chunk);
                AkPlayActionParams = AkPlayActionParams.Create(chunk);
            }
            else if (ActionType == ActionType.SetState)
            {
                AkPropBundle0 = AkPropBundle.Create(chunk);
                AkPropBundle1 = AkPropBundle.Create(chunk);
                AkSetStateParams = AkSetStateParams.Create(chunk);
            }
        }

        public override byte[] GetAsByteArray()
        {
            if (ActionType != ActionType.Play)
                throw new Exception("Unsupported action type");

            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)ActionType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(IdExt, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(IdExt4, out _));
            memStream.Write(AkPropBundle0.GetAsBytes());
            memStream.Write(AkPropBundle1.GetAsBytes());
            memStream.Write(AkPlayActionParams.GetAsBytes());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkAction_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSize()
        {
            Size = HircHeaderSize + 2 + 4 + 1 + AkPropBundle0.GetSize() + AkPropBundle1.GetSize() + AkPlayActionParams.ComputeSize();
        }
        public ActionType GetActionType() => ActionType;
        public uint GetChildId() => IdExt;
        public uint GetStateGroupId() => AkSetStateParams.UlStateGroupId;
    }

    public class AkPlayActionParams
    {
        public byte ByBitVector { get; set; }
        public uint BankId { get; set; }

        public static AkPlayActionParams Create(ByteChunk chunk)
        {
            return new AkPlayActionParams()
            {
                ByBitVector = chunk.ReadByte(),
                BankId = chunk.ReadUInt32(),
            };
        }

        internal static uint ComputeSize()
        {
            return 5;
        }

        public byte[] GetAsBytes()
        {
            var allbytes = new List<byte>();
            allbytes.AddRange(ByteParsers.Byte.EncodeValue(ByBitVector, out _));
            allbytes.AddRange(ByteParsers.UInt32.EncodeValue(BankId, out _));
            return allbytes.ToArray();
        }
    }


    public class AkSetStateParams
    {
        public uint UlStateGroupId { get; set; }
        public uint UlTargetStateId { get; set; }

        public static AkSetStateParams Create(ByteChunk chunk)
        {
            return new AkSetStateParams()
            {
                UlStateGroupId = chunk.ReadUInt32(),
                UlTargetStateId = chunk.ReadUInt32(),
            };
        }

        internal static uint ComputeSize()
        {
            return 8;
        }

        public byte[] GetAsBytes()
        {
            var allbytes = new List<byte>();
            allbytes.AddRange(ByteParsers.UInt32.EncodeValue(UlStateGroupId, out _));
            allbytes.AddRange(ByteParsers.UInt32.EncodeValue(UlTargetStateId, out _));
            return allbytes.ToArray();
        }
    }
}
