using System.Collections.Generic;

namespace Shared.Ui.Common.ToolTipSystem
{
    public enum ToolTipEnum
    {
        None,

        Kitbash_WsMaterial_Emissive_Strength
    }

    public static class ToolTips
    {
        private readonly static Dictionary<ToolTipEnum, string> s_toolTips = new() 
        {
            {ToolTipEnum.None, ""},

        };

        public static Dictionary<ToolTipEnum, string> List { get => s_toolTips; }
    }
   
}
