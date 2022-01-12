using CommonControls.Common;
using CommonControls.MathViews;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        public NotifyAttr<bool> UnamppedBonesFromOriginal { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ApplyRelativeScale { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<string> SavePrefix { get; set; } = new NotifyAttr<string>("cust_");

        public ComboBox<uint> AnimationOutputFormat { get; set; } = new ComboBox<uint>(new uint[] {5,6,7}, 7);
        public ComboBox<MasterEnum> TimeMaster { get; set; } = new ComboBox<MasterEnum>(new MasterEnum[] { MasterEnum.Source, MasterEnum.Target }, MasterEnum.Source);
        public ComboBox<LengthMatchingModeEnum> LengthMatchingMode { get; set; } = new ComboBox<LengthMatchingModeEnum>(new LengthMatchingModeEnum[] { LengthMatchingModeEnum.Smart, LengthMatchingModeEnum .Loop, LengthMatchingModeEnum.Time }, LengthMatchingModeEnum.Smart);
        public ComboBox<CombineModeEnum> CombineMode { get; set; } = new ComboBox<CombineModeEnum>(new CombineModeEnum[] { CombineModeEnum.Add, CombineModeEnum.Replace }, CombineModeEnum.Replace);
        public ComboBox<CopyModeEnum> CopyMode { get; set; } = new ComboBox<CopyModeEnum>(new CopyModeEnum[] { CopyModeEnum.World, CopyModeEnum.Local }, CopyModeEnum.World);

        public DoubleViewModel StartTime { get; set; } = new DoubleViewModel(0);

        public DoubleViewModel StopTime { get; set; } = new DoubleViewModel(0);
    }

    public enum MasterEnum
    { 
        Source,
        Target
    }

    public enum LengthMatchingModeEnum
    {
        Smart,
        Time,
        Loop
    }

    public enum CombineModeEnum
    { 
        Add,
        Replace,
        OnlySource
    }

    public enum CopyModeEnum
    {
        World,
        Local
    }
}
