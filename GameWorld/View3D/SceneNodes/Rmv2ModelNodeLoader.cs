using System.IO;
using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Services;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2ModelNodeLoader
    {
        private readonly MeshBuilderService _meshBuilderService;
        private readonly PackFileService _packFileService;
        private readonly CapabilityMaterialFactory _capabilityMaterialFactory;

        public Rmv2ModelNodeLoader(MeshBuilderService meshBuilderService, PackFileService packFileService, CapabilityMaterialFactory materialFactory)
        {
            _meshBuilderService = meshBuilderService;
            _packFileService = packFileService;
            _capabilityMaterialFactory = materialFactory;
        }

        public void CreateModelNodesFromFile(Rmv2ModelNode outputNode, RmvFile model, AnimationPlayer animationPlayer, string modelFullPath, WsModelFile? wsModel = null)
        {
            WsModelMaterialProvider wsMaterialProvider;
            if(wsModel != null)
                wsMaterialProvider = WsModelMaterialProvider.CreateFromWsModel(_packFileService, wsModel);
            else
                wsMaterialProvider = WsModelMaterialProvider.CreateFromModelPath(_packFileService, modelFullPath);

            outputNode.Model = model;
            for (var lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                if (lodIndex >= outputNode.Children.Count)
                    outputNode.AddObject(new Rmv2LodNode("Lod " + lodIndex, lodIndex));

                var lodNode = outputNode.Children[lodIndex];
                for (var modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var rmvModel = model.ModelList[lodIndex][modelIndex];
                    var geometry = _meshBuilderService.BuildMeshFromRmvModel(rmvModel, model.Header.SkeletonName);
                    
                    var wsModelMaterial = wsMaterialProvider.GetModelMaterial(lodIndex, modelIndex); 
                    var shader = _capabilityMaterialFactory.Create(rmvModel.Material, wsModelMaterial);

                    // This if statement is for Pharaoh Total War, the base game models do not have a model name by default so I am grabbing it
                    // from the model file path.
                    if (string.IsNullOrWhiteSpace(rmvModel.Material.ModelName))
                        rmvModel.Material.ModelName = Path.GetFileNameWithoutExtension(modelFullPath);

                    var node = new Rmv2MeshNode(geometry, rmvModel.Material, shader, animationPlayer);
                    lodNode.AddObject(node);
                }
            }
        }
    }
}


