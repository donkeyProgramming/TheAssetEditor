using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkAction_V112 : HircItem, ICAkAction
    {
        public AkActionType ActionType { get; set; }
        public uint IdExt { get; set; }
        public byte IdExt4 { get; set; }
        public AkPropBundle_V112 AkPropBundle0 { get; set; } = new AkPropBundle_V112();
        public AkPropBundle_V112 AkPropBundle1 { get; set; } = new AkPropBundle_V112();
        public PlayActionParams_V112? PlayActionParams { get; set; }
        public StateActionParams_V112? StateActionParams { get; set; }

        protected override void ReadData(ByteChunk chunk)
        {
            ActionType = (AkActionType)chunk.ReadUShort();
            IdExt = chunk.ReadUInt32();
            IdExt4 = chunk.ReadByte();
            AkPropBundle0. ReadData(chunk);
            AkPropBundle1.ReadData(chunk);

            if (ActionType == AkActionType.Play)
                PlayActionParams = PlayActionParams_V112.ReadData(chunk);
            else if (ActionType == AkActionType.SetState)
                StateActionParams = StateActionParams_V112.ReadData(chunk);
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)ActionType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(IdExt, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(IdExt4, out _));
            memStream.Write(AkPropBundle0.ReadData());
            memStream.Write(AkPropBundle1.ReadData());

            if (ActionType == AkActionType.Play)
                memStream.Write(PlayActionParams!.WriteData());
            else if (ActionType == AkActionType.SetState)
                throw new NotSupportedException("Users probably don't need this complexity.");

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkAction_V112();
            sanityReload.ReadHirc(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            var actionTypeSize = ByteHelper.GetPropertyTypeSize(ActionType);
            var idExtSize = ByteHelper.GetPropertyTypeSize(IdExt);
            var idExt4Size = ByteHelper.GetPropertyTypeSize(IdExt4);
            var akPropBundle0Size = AkPropBundle0.GetSize();
            var akPropBundle1Size = AkPropBundle1.GetSize();

            if (ActionType == AkActionType.Play)
            {
                var playActionParamsSize = PlayActionParams.GetSize();
                SectionSize = idSize + actionTypeSize + idExtSize + idExt4Size + akPropBundle0Size + akPropBundle1Size + playActionParamsSize;
            }
            else if (ActionType == AkActionType.SetState)
                throw new NotSupportedException("Users probably don't need this complexity.");
        }

        public AkActionType GetActionType() => ActionType;
        public uint GetChildId() => IdExt;
        public uint GetStateGroupId() => StateActionParams.StateGroupId;

        public class PlayActionParams_V112
        {
            public byte BitVector { get; set; }
            public uint FileId { get; set; }

            public static PlayActionParams_V112 ReadData(ByteChunk chunk)
            {
                return new PlayActionParams_V112()
                {
                    BitVector = chunk.ReadByte(),
                    FileId = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(FileId, out _));
                return memStream.ToArray();
            }

            public uint GetSize()
            {
                var bitVectorSize = ByteHelper.GetPropertyTypeSize(BitVector);
                var fileIdSize = ByteHelper.GetPropertyTypeSize(FileId);
                return bitVectorSize + fileIdSize;
            }
        }

        public class StateActionParams_V112
        {
            public uint StateGroupId { get; set; }
            public uint TargetStateId { get; set; }

            public static StateActionParams_V112 ReadData(ByteChunk chunk)
            {
                return new StateActionParams_V112()
                {
                    StateGroupId = chunk.ReadUInt32(),
                    TargetStateId = chunk.ReadUInt32()
                };
            }
        }
    }
}
