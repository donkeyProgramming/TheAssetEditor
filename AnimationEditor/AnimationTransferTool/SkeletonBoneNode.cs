using Common;
using CommonControls.Common;
using CommonControls.MathViews;
using System.Collections.ObjectModel;

namespace AnimationEditor.AnimationTransferTool
{
    public class SkeletonBoneNode : NotifyPropertyChangedImpl
    {
        public NotifyAttr<int> BoneIndex { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> ParnetBoneIndex { get; set; } = new NotifyAttr<int>(-1);
        public NotifyAttr<string> BoneName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> HasMapping { get; set; } = new NotifyAttr<bool>(false);

        public SkeletonBoneNode(string boneName, int boneIndex, int parentBoneIndex)
        {
            BoneName.Value = boneName;
            BoneIndex.Value = boneIndex;
            ParnetBoneIndex.Value = parentBoneIndex;
        }

        public NotifyAttr<bool> IsLocalOffset { get; set; } = new NotifyAttr<bool>(false);  // Not implemented, testing
        public DoubleViewModel BoneLengthMult { get; set; } = new DoubleViewModel(1);
        public Vector3ViewModel RotationOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel TranslationOffset { get; set; } = new Vector3ViewModel(0);

        public NotifyAttr<bool> ForceSnapToWorld { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> FreezeTranslation { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> FreezeRotation { get; set; } = new NotifyAttr<bool>(false);

        public NotifyAttr<bool> ApplyTranslation { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<bool> ApplyRotation { get; set; } = new NotifyAttr<bool>(true);

        public SkeletonBoneNode _selectedRelativeBone;
        public SkeletonBoneNode SelectedRelativeBone
        {
            get { return _selectedRelativeBone; }
            set { SetAndNotify(ref _selectedRelativeBone, value); }
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
    }
}
