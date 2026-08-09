using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    public partial class SaveCampaignBinAsViewModel : ObservableObject
    {
        public string FolderHint { get; }

        [ObservableProperty] string _fileName;

        public SaveCampaignBinAsViewModel(string folder, string initialFileName)
        {
            FolderHint = $@"Saved under {folder}\ - only the file name below is yours to choose.";
            FileName = initialFileName;
        }
    }
}
