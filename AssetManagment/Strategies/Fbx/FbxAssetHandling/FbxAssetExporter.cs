// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AssetManagement.AssetBuilders;
using CommonControls.Services;
using CommonControls.Interfaces.AssetManagement;
using AssetManagement.Strategies.Fbx;


namespace AssetManagment.Strategies.Fbx.FbxAssetHandling
{
    public class FbxAssetExporter : IAssetExporter
    {
        public string[] Formats => new string[] { ".fbx" };

        private readonly PackFileService _packFileService;

        public FbxAssetExporter(PackFileService pfs)
        {
            _packFileService = pfs;
        }

        // TODO: what todo about return value not sure if the FBX SDK CAN return a binary FBX file in memory
        public byte[] ExportAsset(AssetManagerData inputData)
        {
            var sceneBuilder = new SceneContainerBuilder();
            sceneBuilder.SetSkeleton(inputData.skeletonFile);
            sceneBuilder.AddMeshList(inputData.RigidModelFile, inputData.skeletonFile);          
            
            SceneExporter.ExportScene(sceneBuilder.CurrentSceneContainer, inputData.DestinationPath);

            return null; // TODO: Do I need to return FBX as binary data ALSO, when "SaveScene()" stored it on disk?            
        }
    }

}
