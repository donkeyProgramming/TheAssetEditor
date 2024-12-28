using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.AnimatioReTarget.Editor.Saving
{
    public partial class SaveSettings : ObservableObject
    {
        public List<uint> PossibleAnimationFormats = [5, 6, 7];

        [ObservableProperty] string _savePrefix = "prefix_";
        [ObservableProperty] uint _animationFormat = 7;
        [ObservableProperty] bool _useGeneratedSkeleton = false;
        [ObservableProperty] string _scaledSkeletonName = "";
    }
}
