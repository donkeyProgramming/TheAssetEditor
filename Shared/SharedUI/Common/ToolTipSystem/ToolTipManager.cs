using System;
using System.Collections.Generic;

namespace Shared.Ui.Common.ToolTipSystem
{
    public static class ToolTipManager
    {
        readonly static Dictionary<ToolTipEnum, string> _toolTips = new Dictionary<ToolTipEnum, string> { { ToolTipEnum.None, "" } };

        static void RegisterToolTip(ToolTipEnum enumValue, string toolTip)
        {
            if (_toolTips.ContainsKey(enumValue))
                throw new Exception($"{nameof(ToolTipManager)} alread have been assigned {enumValue}. Current value is '{_toolTips[enumValue]}'");
            _toolTips.Add(enumValue, toolTip);
        }
        static string GetToolTip(ToolTipEnum enumValue) => _toolTips[enumValue];

        public static void Validate()
        {
            foreach (ToolTipEnum item in Enum.GetValues(typeof(ToolTipEnum)))
            {
                var result = _toolTips.TryGetValue(item, out var toolTip);
                if (result == false)
                    throw new Exception($"ToolTipEnum {item} has not been assigned!");
            }
        }
    }
   
}
