using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkAction_V136 : HircItem, ICAkAction
    {
        public AkActionType ActionType { get; set; }
        public uint IdExt { get; set; }
        public byte IdExt4 { get; set; }
        public AkPropBundle_V136 AkPropBundle0 { get; set; } = new AkPropBundle_V136();
        public AkPropBundle_V136 AkPropBundle1 { get; set; } = new AkPropBundle_V136();
        public PlayActionParams_V136? PlayActionParams { get; set; }
        public ActiveActionParams_V136? ActiveActionParams { get; set; }
        public StateActionParams_V136? StateActionParams { get; set; }

        protected override void ReadData(ByteChunk chunk)
        {
            ActionType = (AkActionType)chunk.ReadUShort();
            IdExt = chunk.ReadUInt32();
            IdExt4 = chunk.ReadByte();
            AkPropBundle0.ReadData(chunk);
            AkPropBundle1.ReadData(chunk);

            if (ActionType == AkActionType.Play)
                PlayActionParams = PlayActionParams_V136.ReadData(chunk);
            else if (ActionType == AkActionType.Pause_E_O)
                ActiveActionParams = ActiveActionParams_V136.ReadData(chunk, ActionType);
            else if (ActionType == AkActionType.Resume_E_O)
                ActiveActionParams = ActiveActionParams_V136.ReadData(chunk, ActionType);
            else if (ActionType == AkActionType.Stop_E_O)
                ActiveActionParams = ActiveActionParams_V136.ReadData(chunk, ActionType);
            else if (ActionType == AkActionType.SetState)
                StateActionParams = StateActionParams_V136.ReadData(chunk);
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)ActionType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(IdExt, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(IdExt4, out _));
            memStream.Write(AkPropBundle0.WriteData());
            memStream.Write(AkPropBundle1.WriteData());

            if (ActionType == AkActionType.Play)
                memStream.Write(PlayActionParams!.WriteData());
            else if (ActionType == AkActionType.Pause_E_O)
                memStream.Write(ActiveActionParams!.WriteData(ActionType));
            else if (ActionType == AkActionType.Resume_E_O)
                memStream.Write(ActiveActionParams!.WriteData(ActionType));
            else if (ActionType == AkActionType.Stop_E_O)
                memStream.Write(ActiveActionParams!.WriteData(ActionType));

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkAction_V136();
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
                var playActionParamsSize = PlayActionParams!.GetSize();
                SectionSize = (ushort)(idSize + actionTypeSize + idExtSize + idExt4Size + akPropBundle0Size + akPropBundle1Size + playActionParamsSize);
            }
            else if (ActionType == AkActionType.Pause_E_O)
            {
                var pauseActionParamsSize = ActiveActionParams!.GetSize(AkActionType.Pause_E_O);
                SectionSize = (ushort)(idSize + actionTypeSize + idExtSize + idExt4Size + akPropBundle0Size + akPropBundle1Size + pauseActionParamsSize);
            }
            else if (ActionType == AkActionType.Resume_E_O)
            {
                var resumeActionParamsSize = ActiveActionParams!.GetSize(AkActionType.Resume_E_O);
                SectionSize = (ushort)(idSize + actionTypeSize + idExtSize + idExt4Size + akPropBundle0Size + akPropBundle1Size + resumeActionParamsSize);
            }
            else if (ActionType == AkActionType.Stop_E_O)
            {
                var stopActionParamsSize = ActiveActionParams!.GetSize(AkActionType.Stop_E_O);
                SectionSize = (ushort)(idSize + actionTypeSize + idExtSize + idExt4Size + akPropBundle0Size + akPropBundle1Size + stopActionParamsSize);
            }
        }

        public AkActionType GetActionType() => ActionType;
        public uint GetChildId() => IdExt;
        public uint GetStateGroupId() => StateActionParams!.StateGroupId;

        public class PlayActionParams_V136
        {
            public byte BitVector { get; set; }
            public uint BankId { get; set; }

            public static PlayActionParams_V136 ReadData(ByteChunk chunk)
            {
                return new PlayActionParams_V136()
                {
                    BitVector = chunk.ReadByte(),
                    BankId = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(BankId, out _));
                return memStream.ToArray();
            }

            public uint GetSize()
            {
                var bitVectorSize = ByteHelper.GetPropertyTypeSize(BitVector);
                var fileIdSize = ByteHelper.GetPropertyTypeSize(BankId);
                return bitVectorSize + fileIdSize;
            }
        }

        public class ActiveActionParams_V136
        {
            public byte BitVector { get; set; }
            public PauseActionSpecificParams_V136 PauseActionSpecificParams { get; set; } = new PauseActionSpecificParams_V136();
            public ResumeActionSpecificParams_V136 ResumeActionSpecificParams { get; set; } = new ResumeActionSpecificParams_V136();
            public StopActionSpecificParams_V136 StopActionSpecificParams { get; set; } = new StopActionSpecificParams_V136();
            public ExceptParams_V136 ExceptParams { get; set; } = new ExceptParams_V136();

            public static ActiveActionParams_V136 ReadData(ByteChunk chunk, AkActionType actionType)
            {
                var activeActionParams = new ActiveActionParams_V136();
                activeActionParams.BitVector = chunk.ReadByte();

                if (actionType == AkActionType.Pause_E_O)
                    activeActionParams.PauseActionSpecificParams = PauseActionSpecificParams_V136.ReadData(chunk);
                else if(actionType == AkActionType.Resume_E_O)
                    activeActionParams.ResumeActionSpecificParams = ResumeActionSpecificParams_V136.ReadData(chunk);
                else if (actionType == AkActionType.Stop_E_O)
                    activeActionParams.StopActionSpecificParams = StopActionSpecificParams_V136.ReadData(chunk);

                activeActionParams.ExceptParams = ExceptParams_V136.ReadData(chunk);
                return activeActionParams;
            }

            public byte[] WriteData(AkActionType actionType)
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));

                if (actionType == AkActionType.Pause_E_O)
                    memStream.Write(PauseActionSpecificParams.WriteData());
                else if (actionType == AkActionType.Resume_E_O)
                    memStream.Write(ResumeActionSpecificParams.WriteData());
                else if (actionType == AkActionType.Stop_E_O)
                    memStream.Write(StopActionSpecificParams.WriteData());

                memStream.Write(ExceptParams.WriteData());
                return memStream.ToArray();
            }

            public uint GetSize(AkActionType actionType)
            {
                var bitVectorSize = ByteHelper.GetPropertyTypeSize(BitVector);

                uint actionSpecificParamsSize = 0;
                if(actionType == AkActionType.Pause_E_O)
                    actionSpecificParamsSize = PauseActionSpecificParams.GetSize();
                else if(actionType == AkActionType.Resume_E_O)
                    actionSpecificParamsSize = ResumeActionSpecificParams.GetSize();
                else if (actionType == AkActionType.Stop_E_O)
                    actionSpecificParamsSize = StopActionSpecificParams.GetSize();

                var exceptParamsSize = ExceptParams.GetSize();
                return bitVectorSize + actionSpecificParamsSize + exceptParamsSize;
            }

            public class PauseActionSpecificParams_V136
            {
                public byte BitVector { get; set; }

                public static PauseActionSpecificParams_V136 ReadData(ByteChunk chunk)
                {
                    return new PauseActionSpecificParams_V136()
                    {
                        BitVector = chunk.ReadByte()
                    };
                }

                public byte[] WriteData()
                {
                    return ByteParsers.Byte.EncodeValue(BitVector, out _);
                }

                public uint GetSize()
                {
                    return ByteHelper.GetPropertyTypeSize(BitVector);
                }
            }

            public class ResumeActionSpecificParams_V136
            {
                public byte BitVector { get; set; }

                public static ResumeActionSpecificParams_V136 ReadData(ByteChunk chunk)
                {
                    return new ResumeActionSpecificParams_V136()
                    {
                        BitVector = chunk.ReadByte()
                    };
                }

                public byte[] WriteData()
                {
                    return ByteParsers.Byte.EncodeValue(BitVector, out _);
                }

                public uint GetSize()
                {
                    return ByteHelper.GetPropertyTypeSize(BitVector);
                }
            }

            public class StopActionSpecificParams_V136
            {
                public byte BitVector { get; set; }

                public static StopActionSpecificParams_V136 ReadData(ByteChunk chunk)
                {
                    return new StopActionSpecificParams_V136()
                    {
                        BitVector = chunk.ReadByte()
                    };
                }

                public byte[] WriteData()
                {
                    return ByteParsers.Byte.EncodeValue(BitVector, out _);
                }

                public uint GetSize()
                {
                    return ByteHelper.GetPropertyTypeSize(BitVector);
                }
            }

            public class ExceptParams_V136
            {
                public byte ExceptionListSize { get; set; }
                public List<Exception_V136> ExceptionList { get; set; } = [];

                public static ExceptParams_V136 ReadData(ByteChunk chunk)
                {
                    var exceptParams = new ExceptParams_V136
                    {
                        ExceptionListSize = chunk.ReadByte()
                    };

                    for (var i = 0; i < exceptParams.ExceptionListSize; i++)
                        exceptParams.ExceptionList.Add(Exception_V136.ReadData(chunk));

                    return exceptParams;
                }

                public byte[] WriteData()
                {
                    using var memStream = new MemoryStream();
                    memStream.Write(ByteParsers.Byte.EncodeValue((byte)ExceptionList.Count, out _));

                    foreach (var exception in ExceptionList)
                        memStream.Write(exception.WriteData());

                    return memStream.ToArray();
                }

                public uint GetSize()
                {
                    var size = ByteHelper.GetPropertyTypeSize(ExceptionListSize);
                    foreach (var exception in ExceptionList)
                        size += exception.GetSize();

                    return size;
                }
            }

            public class Exception_V136
            {
                public uint Id { get; set; }
                public byte IsBus { get; set; }

                public static Exception_V136 ReadData(ByteChunk chunk)
                {
                    return new Exception_V136
                    {
                        Id = chunk.ReadUInt32(),
                        IsBus = chunk.ReadByte()
                    };
                }

                public byte[] WriteData()
                {
                    using var memStream = new MemoryStream();
                    memStream.Write(ByteParsers.UInt32.EncodeValue(Id, out _));
                    memStream.Write(ByteParsers.Byte.EncodeValue(IsBus, out _));
                    return memStream.ToArray();
                }

                public uint GetSize()
                {
                    return ByteHelper.GetPropertyTypeSize(Id) +
                           ByteHelper.GetPropertyTypeSize(IsBus);
                }
            }
        }

        public class StateActionParams_V136
        {
            public uint StateGroupId { get; set; }
            public uint TargetStateId { get; set; }

            public static StateActionParams_V136 ReadData(ByteChunk chunk)
            {
                return new StateActionParams_V136()
                {
                    StateGroupId = chunk.ReadUInt32(),
                    TargetStateId = chunk.ReadUInt32()
                };
            }
        }
    }
}
