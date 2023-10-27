using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Interfaces.AssetManagement;
using System.IO;
using AssetManagement.Strategies.Fbx.ImportDialog.DataModels;
using CommonControls.Services;
using AssetManagement.Strategies.Fbx.ImportDialog.ViewModels;
using AssetManagement.AssetBuilders;

namespace AssetManagement.Strategies.Fbx.AssetHandling
{
    public class FbxAssetImporter : IAssetImporter
    {
        public string[] Formats => new string[] { ".fbx" };

        private readonly PackFileService _packFileService;

        public FbxAssetImporter(PackFileService pfs)
        {
            _packFileService = pfs;
        }

        public PackFile ImportAsset(string diskFilePath)
        {
            var sceneContainer = SceneLoader.LoadScene(diskFilePath);
            if (sceneContainer == null)
                return null;

            var fbxSettings = new FbxSettingsModel()
            {
                SkeletonName = sceneContainer.SkeletonName,
                FileInfoData = sceneContainer.FileInfoData
            };

            if (!FBXSettingsViewModel.ShowImportDialog(_packFileService, fbxSettings))
            {
                return null;
            }

            var rmv2File = RmvFileBuilder.ConvertToRmv2(sceneContainer.Meshes, fbxSettings.SkeletonPackFile);
            var factory = ModelFactory.Create();
            var buffer = factory.Save(rmv2File);

            var rmv2FileName = $"{Path.GetFileNameWithoutExtension(diskFilePath)}.rigid_model_v2";
            var packFile = new PackFile(rmv2FileName, new MemorySource(buffer));
            return packFile;
        }
    }
}
