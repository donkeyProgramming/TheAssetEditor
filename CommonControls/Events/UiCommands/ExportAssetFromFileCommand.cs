using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.WsModel;
using CommonControls.Interfaces.AssetManagement;
using CommonControls.Services;

namespace CommonControls.Events.UiCommands
{
    public class InputData
    {
        public RmvFile RigidModelFile { set; get; }
        public WsMaterial wsmodelFile { set; get; }
        public AnimationFile skeletonFile { set; get; }
        public AnimationFile animationFile { set; get; }
    }

    public class ExportAssetFromFileCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;
        private readonly IAssetManagementFactory _assetManagementFactory;

        public ExportAssetFromFileCommand(PackFileService packFileService, IAssetManagementFactory assetManagementFactory)
        {
            _packFileService = packFileService;
            _assetManagementFactory = assetManagementFactory;
        }

        /// <summary>
        /// Exports complete asset from packfile, input path can WSMODEL or RMV2
        /// </summary>        
        public void Execute(PackFileContainer fileOwner, string pathModel, string pathAnimationClip = "")
        {

            var inputFiles = ExportHelper.FetchInputFiles(_packFileService, pathModel);
            // ....
        }
    }

}
