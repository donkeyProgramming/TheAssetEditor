using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.AnimatioReTarget.Editor.Saving
{
    public partial class SaveSettings : ObservableObject
    {
        public List<uint> PossibleAnimationFormats { get; } = [5, 6, 7, 8];   // a property: WPF cannot bind to a field

        [ObservableProperty] string _savePrefix = "prefix_";
        [ObservableProperty] uint _animationFormat = 7;
        [ObservableProperty] bool _useGeneratedSkeleton = false;
        [ObservableProperty] string _scaledSkeletonName = "";

        [ObservableProperty] bool _batchUseSelectedFolder;
        [ObservableProperty] string _batchTargetFolder = "";
        [ObservableProperty] bool _batchOverwriteExisting;

        // For the "source path" radio button; unticking it is the other button's job
        public bool BatchUseSourcePath
        {
            get => !BatchUseSelectedFolder;
            set
            {
                if (value)
                    BatchUseSelectedFolder = false;
            }
        }

        partial void OnBatchUseSelectedFolderChanged(bool value) => OnPropertyChanged(nameof(BatchUseSourcePath));
    }
}
