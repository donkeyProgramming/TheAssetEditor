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
            {ToolTipEnum.Kitbash_WsMaterial_Emissive_Strength, "There is a bug which causes the rendering to be to dark.\nIn game is about 3x as bright!"}
        };

        public static Dictionary<ToolTipEnum, string> List { get => s_toolTips; }
    }
   
}
