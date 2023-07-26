using CommonControls.FileTypes.Animation;
namespace AssetManagement.Strategies.Fbx.Models
{
    public class FbxSettingsModel
    {
        public AnimationFile SkeletonFile { get; set; } = null;
        public string SkeletonFileName { get; set; } = "";
        public string SkeletonName { get; set; } = "";
        public bool UseAutoRigging { get; set; } = true;

    }
}
