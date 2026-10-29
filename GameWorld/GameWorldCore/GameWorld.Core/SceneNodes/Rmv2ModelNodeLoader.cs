using System.IO;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2ModelNodeLoader
    {
        private readonly ILogger _logger;
        private readonly MeshBuilderService _meshBuilderService;
        private readonly CapabilityMaterialFactory _capabilityMaterialFactory;
        private readonly IModelAssetResolver _modelAssetResolver;

        public Rmv2ModelNodeLoader(
            MeshBuilderService meshBuilderService,
            IPackFileService packFileService,
            CapabilityMaterialFactory materialFactory,
            IStandardDialogs exceptionService,
            IScopedLogger scopedLogger,
            IModelAssetResolver? modelAssetResolver = null)
        {
            _logger = scopedLogger.ForContext<Rmv2ModelNodeLoader>();
            _meshBuilderService = meshBuilderService;
            _capabilityMaterialFactory = materialFactory;
            _modelAssetResolver = modelAssetResolver ?? new ModelAssetResolver(packFileService);
        }

        public List<Rmv2LodNode> CreateModelNodesFromFile(RmvFile model, string modelFullPath, bool onlyLoadRootNode, WsModelFile? wsModel = null)
        {
            var resolvedMaterials = _modelAssetResolver.ResolveMaterials(model, wsModel, modelFullPath);
            LogResolutionDiagnostics(resolvedMaterials.Diagnostics);
            return CreateModelNodes(model, modelFullPath, onlyLoadRootNode, resolvedMaterials.PartsByLod);
        }

        public List<Rmv2LodNode> CreateModelNodesFromAsset(ResolvedModelAsset asset, string modelFullPath, bool onlyLoadRootNode)
        {
            ArgumentNullException.ThrowIfNull(asset);
            LogResolutionDiagnostics(asset.Diagnostics);
            return CreateModelNodes(asset.Model, modelFullPath, onlyLoadRootNode, asset.PartsByLod);
        }

        private List<Rmv2LodNode> CreateModelNodes(
            RmvFile model,
            string modelFullPath,
            bool onlyLoadRootNode,
            IReadOnlyList<IReadOnlyList<ResolvedModelPart>> resolvedPartsByLod)
        {
            var output = new List<Rmv2LodNode>();
            for (var lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var currentNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                output.Add(currentNode);

                for (var modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var rmvModel = model.ModelList[lodIndex][modelIndex];
                    var geometry = _meshBuilderService.BuildMeshFromRmvModel(rmvModel, model.Header.SkeletonName);

                    var resolvedPart = resolvedPartsByLod.Count > lodIndex && resolvedPartsByLod[lodIndex].Count > modelIndex
                        ? resolvedPartsByLod[lodIndex][modelIndex]
                        : null;
                    var shader = resolvedPart == null
                        ? _capabilityMaterialFactory.Create(rmvModel.Material)
                        : _capabilityMaterialFactory.Create(resolvedPart.Material.SourceMaterial, resolvedPart.Material.WsModelMaterial);
    
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

        private void LogResolutionDiagnostics(IReadOnlyList<string> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
                _logger.Here().Warning(diagnostic);
        }
    }
}

