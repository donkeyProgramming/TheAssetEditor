using Shared.GameFormats.Wwise.Hirc.Shared;

namespace Shared.GameFormats.Wwise.Hirc
{
    public interface ICAkEvent
    {
        public List<uint> GetActionIds();
    }

    public interface ICAkSound
    {
        public uint GetDirectParentId();
        public uint GetSourceId();
        public SourceType GetStreamType();
    }

    public interface ICAkAction
    {
        public ActionType GetActionType();
        public uint GetChildId();
        public uint GetStateGroupId();
    }

    public interface ICAkDialogueEvent
    {
        public ArgumentList ArgumentList { get; }
        public AkDecisionTree AkDecisionTree { get; }
    }

    public interface ICAkActorMixer
    {
        public List<uint> GetChildren();
        public uint GetDirectParentId();
    }

    public interface ICAkSwitchCntr
    {
        public uint GetDirectParentId();
        public List<ICAkSwitchPackage> SwitchList { get; }
        uint UlGroupId { get; }
        uint UlDefaultSwitch { get; }

        public interface ICAkSwitchPackage
        {
            uint SwitchId { get; }
            List<uint> NodeIdList { get; }
        }
    }

    public interface ICAkMusicTrack
    {
        public List<uint> GetChildren();
    }

    public interface ICAkLayerCntr
    {
        public List<uint> GetChildren();
        public uint GetDirectParentId();
    }

    public interface ICAkRanSeqCnt
    {
        public uint GetParentId();
        public List<uint> GetChildren();
    }
}
