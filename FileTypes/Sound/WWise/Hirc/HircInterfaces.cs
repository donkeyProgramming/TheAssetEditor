using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
{
    public abstract class CAkEvent : HircItem
    {
        public abstract List<uint> GetActionIds();
    }

    public abstract class CAkSound : HircItem
    {
        public abstract uint GetParentId();
        public abstract uint GetSourceId();
    }

    public abstract class CAkAction : HircItem
    {
        public abstract ActionType GetActionType();
        public abstract uint GetSoundId();
    }

    public abstract class CAkRanSeqCnt : HircItem
    {
        public abstract uint GetParentId();
        public abstract List<uint> GetChildren();
    }


    public abstract class CADialogEvent_abs : HircItem
    {
        public abstract List<ChildNode> GetChildren();

        public abstract class ChildNode
        {
            public abstract uint GetKey();

            public abstract List<ChildNode> GetChildren();
            public abstract List<SoundNode> GetSoundNodes();
        }

        public abstract class SoundNode
        {
            public abstract uint GetKey();
            public abstract uint GetAudioNodeId();
        }
    }
}