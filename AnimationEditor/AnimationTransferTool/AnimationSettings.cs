using CommonControls.Common;
using CommonControls.MathViews;

namespace AnimationEditor.AnimationTransferTool
{
    public class AnimationSettings : NotifyPropertyChangedImpl
    {
        public Vector3ViewModel DisplayOffset { get; set; } = new Vector3ViewModel(0, 0, 2);
        public DoubleViewModel Scale { get; set; } = new DoubleViewModel(1);
        public NotifyAttr<bool> UseScaledSkeletonName { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> ScaledSkeletonName { get; set; } = new NotifyAttr<string>("");
        
        public DoubleViewModel SpeedMult { get; set; } = new DoubleViewModel(1);
        public NotifyAttr<bool> FreezeUnmapped { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ApplyRelativeScale { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<string> SavePrefix { get; set; } = new NotifyAttr<string>("cust_");
    }
}
