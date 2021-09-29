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
}