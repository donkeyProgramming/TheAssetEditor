using Shared.GameFormats.WWise.Hirc.Shared;

namespace Shared.GameFormats.WWise.Hirc
{
    public interface ICAkEvent
    {
        public List<uint> GetActionIds();
    }

    public interface ICAkSound
    {
        public uint GetDirectParentId();
        public uint GetSourceId();
    }

    public interface ICAkAction
    {
        public ActionType GetActionType();
        public uint GetChildId();
        public uint GetStateGroupId();
    }

    public interface ICADialogEvent
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

        uint GroupId { get; }
        uint DefaultSwitch { get; }

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


    // Convert to interfaces 

    public abstract class CAkRanSeqCnt : HircItem
    {
        public abstract uint GetParentId();
        public abstract List<uint> GetChildren();
    }



    public abstract class CAkSwitchCntr : HircItem
    {
        public abstract uint GroupId { get; }
        public abstract uint DefaultSwitch { get; }
        public abstract uint ParentId { get; }
        public abstract List<SwitchListItem> Items { get; }

        public class SwitchListItem
        {
            public uint SwitchId { get; set; }
            public List<uint> ChildNodeIds { get; set; }
        }
    }
}
