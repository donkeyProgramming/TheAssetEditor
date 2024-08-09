using System.IO;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Services;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.SceneNodes
{

    public class Rmv2ModelNodeLoader
    {
        private readonly IGeometryGraphicsContextFactory _contextFactory;
        private readonly PackFileService _packFileService;
        private readonly CapabilityMaterialFactory _capabilityMaterialFactory;

        public Rmv2ModelNodeLoader(IGeometryGraphicsContextFactory contextFactory, PackFileService packFileService, CapabilityMaterialFactory materialFactory)
        {
            _contextFactory = contextFactory;
            _packFileService = packFileService;
            _capabilityMaterialFactory = materialFactory;
        }

        public void CreateModelNodesFromFile(Rmv2ModelNode outputNode, RmvFile model, AnimationPlayer animationPlayer, string modelFullPath)
        {
            var wsModelPath = Path.ChangeExtension(modelFullPath, ".wsmodel");
            var wsModelPackFile = _packFileService.FindFile(wsModelPath);
            WsModelFile? wsModelFile = null;
            if (wsModelPackFile != null)
                wsModelFile = new WsModelFile(wsModelPackFile);

            outputNode.Model = model;
            for (var lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                if (lodIndex >= outputNode.Children.Count)
                    outputNode.AddObject(new Rmv2LodNode("Lod " + lodIndex, lodIndex));

                var lodNode = outputNode.Children[lodIndex];
                for (var modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = MeshBuilderService.BuildMeshFromRmvModel(model.ModelList[lodIndex][modelIndex], model.Header.SkeletonName, _contextFactory.Create());
                    var rmvModel = model.ModelList[lodIndex][modelIndex];

                    var wsModelMaterial = wsModelFile?.MaterialList.FirstOrDefault(x => x.LodIndex == lodIndex && x.PartIndex == modelIndex);
                    var shader = _capabilityMaterialFactory.Create(rmvModel, wsModelMaterial?.MaterialPath);

                    // This if statement is for Pharaoh Total War, the base game models do not have a model name by default so I am grabbing it
                    // from the model file path.
                    if (string.IsNullOrWhiteSpace(rmvModel.Material.ModelName))
                        rmvModel.Material.ModelName = Path.GetFileNameWithoutExtension(modelFullPath);

                    var node = new Rmv2MeshNode(rmvModel.CommonHeader, geometry, rmvModel.Material, animationPlayer, shader)
                    {
                        OriginalFilePath = modelFullPath,
                        OriginalPartIndex = modelIndex,
                        LodIndex = lodIndex
                    };

                    lodNode.AddObject(node);
                }
            }
        }
    }
}


