using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetManagement.Strategies.Fbx.Views.FBXSettings
{
    class LineEditorViewModel
    {
        public NotifyAttr<string> LabelControl { get; set; } = new NotifyAttr<string>($"empty.fbx");
        public NotifyAttr<string> TextBoxControl { get; set; } = new NotifyAttr<string>($"Inches");


    }
}
