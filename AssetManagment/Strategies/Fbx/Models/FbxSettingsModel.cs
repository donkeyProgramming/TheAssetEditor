using CommonControls.FileTypes.Animation;
using AssetManagement.GenericFormats.Managed;

namespace AssetManagement.Strategies.Fbx.Models
{
    public class FbxSettingsModel
    {
        public FileInfoData FileInfoData { get; set; } = new FileInfoData();
        public AnimationFile SkeletonFile { get; set; } = null;
        public string SkeletonFileName { get; set; } = "";
        public string SkeletonName { get; set; } = "";
        public bool UseAutoRigging { get; set; } = true;

    }
}
