using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public interface ICAkEvent
    {
        public List<uint> GetActionIds();
    }

    public interface ICAkSound
    {
        public uint GetDirectParentID();
        public uint GetSourceID();
        public AKBKSourceType GetStreamType();
    }

    public interface ICAkAction
    {
        public AkActionType GetActionType();
        public uint GetChildID();
        public uint GetStateGroupID();
    }

    public interface ICAkDialogueEvent
    {
        public List<IAkGameSync> Arguments { get; }
        IAkDecisionTree AkDecisionTree { get; }

        public interface IAkGameSync
        {
            public uint GroupID { get; set;  }
            public AkGroupType GroupType { get; set; }
            public uint GetSize();
        }

        public interface IAkDecisionTree
        {
            void ReadData(ByteChunk chunk, uint treeDataSize, uint treeDepth);
            byte[] WriteData();
        }
    }

    public interface ICAkActorMixer
    {
        public List<uint> GetChildren();
        public uint GetDirectParentID();
    }

    public interface ICAkSwitchCntr
    {
        public uint GroupID { get; }
        public uint DefaultSwitch { get; }
        public List<ICAkSwitchPackage> SwitchList { get; }

        public interface ICAkSwitchPackage
        {
            public uint SwitchID { get; }
            public List<uint> NodeIdList { get; }
        }

        public uint GetDirectParentID();
    }

    public interface ICAkMusicTrack
    {
        public List<uint> GetChildren();
    }

    public interface ICAkLayerCntr
    {
        public List<uint> GetChildren();
        public uint GetDirectParentID();
    }

    public interface ICAkRanSeqCntr
    {
        public uint GetDirectParentID();
        public List<uint> GetChildren();
    }
}
