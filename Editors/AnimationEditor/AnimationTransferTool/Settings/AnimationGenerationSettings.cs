using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.AnimationVisualEditors.AnimationTransferTool.Settings
{
    public partial class AnimationGenerationSettings : ObservableObject
    {
        [ObservableProperty] float _skeletonScale = 1;
        [ObservableProperty] float _animationSpeedMult = 1;
        [ObservableProperty] bool _applyRelativeScale = true;
        [ObservableProperty] bool _zeroUnmappedBones = false;
    }
}
