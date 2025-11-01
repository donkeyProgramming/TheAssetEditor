using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public interface ICAkEvent
    {
        List<uint> GetActionIds();
    }

    public interface ICAkSound
    {
        uint GetDirectParentId();
        uint GetSourceId();
        AKBKSourceType GetStreamType();
    }

    public interface ICAkAction
    {
        AkActionType GetActionType();
        uint GetChildId();
        uint GetStateGroupId();
    }

    public interface ICAkDialogueEvent
    {
        List<IAkGameSync> Arguments { get; }
        IAkDecisionTree AkDecisionTree { get; }

        public interface IAkGameSync
        {
            uint GroupId { get; set;  }
            AkGroupType GroupType { get; set; }
            uint GetSize();
        }

        public interface IAkDecisionTree
        {
            void ReadData(ByteChunk chunk, uint treeDataSize, uint treeDepth);
            byte[] WriteData();
            IAkDecisionNode GetDecisionTree();
        }

        public interface IAkDecisionNode
        {
            uint GetKey();
            uint GetAudioNodeId();
            int GetChildrenCount();
            IAkDecisionNode GetChildAtIndex(int index);
        }
    }

    public interface ICAkActorMixer
    {
        List<uint> GetChildren();
        uint GetDirectParentId();
    }

    public interface ICAkSwitchCntr
    {
        uint GroupId { get; }
        uint DefaultSwitch { get; }
        List<ICAkSwitchPackage> SwitchList { get; }

        public interface ICAkSwitchPackage
        {
            uint SwitchId { get; }
            List<uint> NodeIdList { get; }
        }

        uint GetDirectParentId();
    }

    public interface ICAkMusicTrack
    {
        List<uint> GetChildren();
    }

    public interface ICAkLayerCntr
    {
        List<uint> GetChildren();
        uint GetDirectParentId();
    }

    public interface ICAkRanSeqCntr
    {
        uint GetDirectParentId();
        List<uint> GetChildren();
    }
}
