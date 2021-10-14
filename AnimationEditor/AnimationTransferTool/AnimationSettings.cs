using Common;
using CommonControls.Common;
using CommonControls.MathViews;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.AnimationTransferTool
{
    public class AnimationSettings : NotifyPropertyChangedImpl
    {
        public Vector3ViewModel OffsetGenerated { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel OffsetTarget { get; set; } = new Vector3ViewModel(0, 0, -2);
        public Vector3ViewModel OffsetSource { get; set; } = new Vector3ViewModel(0, 0, 2);
        public DoubleViewModel Scale { get; set; } = new DoubleViewModel(1);
        public NotifyAttr<bool> UseScaledSkeletonName { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> ScaledSkeletonName { get; set; } = new NotifyAttr<string>("");
        
        public DoubleViewModel SpeedMult { get; set; } = new DoubleViewModel(1);
        public NotifyAttr<bool> FreezeUnmapped { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ApplyRelativeScale { get; set; } = new NotifyAttr<bool>(true);
    }
}
