using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2ModelNodeLoader
    {
        private readonly ILogger _logger = Logging.Create<Rmv2ModelNodeLoader>();
        private readonly MeshBuilderService _meshBuilderService;
        private readonly IPackFileService _packFileService;
        private readonly CapabilityMaterialFactory _capabilityMaterialFactory;
        private readonly IStandardDialogs _standardDialogs;

        public Rmv2ModelNodeLoader(MeshBuilderService meshBuilderService, IPackFileService packFileService, CapabilityMaterialFactory materialFactory, IStandardDialogs exceptionService)
        {
            _meshBuilderService = meshBuilderService;
            _packFileService = packFileService;
            _capabilityMaterialFactory = materialFactory;
            _standardDialogs = exceptionService;
        }

        public List<Rmv2LodNode> CreateModelNodesFromFile(RmvFile model, string modelFullPath, bool onlyLoadRootNode, WsModelFile? wsModel = null)
        {
            WsModelMaterialProvider wsMaterialProvider;
            if(wsModel != null)
                wsMaterialProvider = WsModelMaterialProvider.CreateFromWsModel(_packFileService, _capabilityMaterialFactory, _standardDialogs, wsModel);
            else
                wsMaterialProvider = WsModelMaterialProvider.CreateFromModelPath(_packFileService, _capabilityMaterialFactory, _standardDialogs,  modelFullPath);

            var meshCountInLods = model.ModelList.Select(x => x.Count()).ToArray();
            wsMaterialProvider.ValidateWsModelMaterial(meshCountInLods);

            var output = new List<Rmv2LodNode>();
            for (var lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var currentNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                output.Add(currentNode);

                for (var modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var rmvModel = model.ModelList[lodIndex][modelIndex];
                    var geometry = _meshBuilderService.BuildMeshFromRmvModel(rmvModel, model.Header.SkeletonName);

                    var shader = wsMaterialProvider.ConstructMaterial(lodIndex, modelIndex, rmvModel.Material);
    
                    // This if statement is for Pharaoh Total War, the base game models do not have a model name by default so I am grabbing it
                    // from the model file path.
                    if (string.IsNullOrWhiteSpace(rmvModel.Material.ModelName))
                        rmvModel.Material.ModelName = Path.GetFileNameWithoutExtension(modelFullPath);

                    var node = new Rmv2MeshNode(geometry, rmvModel.Material, shader, null);
                    currentNode.AddObject(node);
                }

                if (onlyLoadRootNode)
                {
                    _logger.Here().Information($"Only loading root node for mesh - {modelFullPath}");
                    break;
                }
            }

            return output;
        }
    }
}


