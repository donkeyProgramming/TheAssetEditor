using Monogame.WpfInterop.ResourceHandling;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using System;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Services;

namespace View3D.SceneNodes
{
    public class Rmv2ModelNodeLoader
    {
        private readonly ILogger _logger = Logging.Create<Rmv2ModelNodeLoader>();

        private readonly ResourceLibrary _resourceLibary;
        private readonly IGeometryGraphicsContextFactory _contextFactory;
        private readonly PackFileService _packFileService;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public Rmv2ModelNodeLoader(ResourceLibrary resourceLibary, IGeometryGraphicsContextFactory contextFactory, PackFileService packFileService, RenderEngineComponent renderEngineComponent, ApplicationSettingsService applicationSettingsService)
        {
            _resourceLibary = resourceLibary;
            _contextFactory = contextFactory;
            _packFileService = packFileService;
            _renderEngineComponent = renderEngineComponent;
            _applicationSettingsService = applicationSettingsService;
        }

        public void CreateModelNodesFromFile(Rmv2ModelNode outputNode, RmvFile model, AnimationPlayer animationPlayer, string modelFullPath)
        {
            outputNode.Model = model;
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                if (lodIndex >= outputNode.Children.Count)
                {
                    outputNode.AddObject(new Rmv2LodNode("Lod " + lodIndex, lodIndex));
                }

                var lodNode = outputNode.Children[lodIndex];
                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = MeshBuilderService.BuildMeshFromRmvModel(model.ModelList[lodIndex][modelIndex], model.Header.SkeletonName, _contextFactory.Create());
                    var rmvModel = model.ModelList[lodIndex][modelIndex];
                    var node = new Rmv2MeshNode(rmvModel.CommonHeader, geometry, rmvModel.Material, animationPlayer, _renderEngineComponent);
                    node.Initialize(_resourceLibary);
                    node.OriginalFilePath = modelFullPath;
                    node.OriginalPartIndex = modelIndex;
                    node.LodIndex = lodIndex;

                    if (_applicationSettingsService.CurrentSettings.AutoResolveMissingTextures)
                    {
                        try
                        {
                            MissingTextureResolver missingTextureResolver = new MissingTextureResolver();
                            missingTextureResolver.ResolveMissingTextures(node, _packFileService);
                        }
                        catch (Exception e)
                        {
                            _logger.Here().Error($"Error while trying to resolve textures from WS model while loading model, {e.Message}");
                        }

                    }

                    lodNode.AddObject(node);
                }
            }
        }
    }
}


