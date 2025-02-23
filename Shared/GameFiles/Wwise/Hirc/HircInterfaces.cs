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
        List<object> Arguments { get; }
        object AkDecisionTree { get; }
    }

    public interface ICAkActorMixer
    {
        public List<uint> GetChildren();
        public uint GetDirectParentID();
    }

    public interface ICAkSwitchCntr
    {
        uint GroupId { get; }
        uint DefaultSwitch { get; }
        public List<ICAkSwitchPackage> SwitchList { get; }

        public interface ICAkSwitchPackage
        {
            uint SwitchId { get; }
            List<uint> NodeIdList { get; }
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

    public interface ICAkRanSeqCnt
    {
        public uint GetDirectParentID();
        public List<uint> GetChildren();
    }
}
