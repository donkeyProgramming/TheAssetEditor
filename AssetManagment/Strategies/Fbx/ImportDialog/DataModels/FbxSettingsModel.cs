using CommonControls.FileTypes.Animation;
using AssetManagement.GenericFormats.DataStructures.Managed;

namespace AssetManagement.Strategies.Fbx.ImportDialog.DataModels
{
    public class FbxSettingsModel
    {
        public FileInfoData FileInfoData { get; set; } = new FileInfoData();
        public AnimationFile SkeletonFile { get; set; } = null;
        public string SkeletonFileName { get; set; } = "";
        public string SkeletonName { get; set; } = "";
        public bool ApplyRigging { get; set; } = true;

    }
}
