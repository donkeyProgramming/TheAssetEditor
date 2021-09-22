using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
{
    public abstract class CAkEvent : HircItem
    {
        public abstract List<uint> GetActionIds();
    }
}
