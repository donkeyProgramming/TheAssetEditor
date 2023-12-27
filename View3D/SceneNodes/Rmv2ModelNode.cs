using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Services;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2ModelNodeLoader
    {
        private readonly ILogger _logger = Logging.Create<Rmv2ModelNodeLoader>();

        private readonly ResourceLibary _resourceLibary;
        private readonly IGeometryGraphicsContextFactory _contextFactory;
        private readonly PackFileService _packFileService;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public Rmv2ModelNodeLoader(ResourceLibary resourceLibary, IGeometryGraphicsContextFactory contextFactory, PackFileService packFileService, RenderEngineComponent renderEngineComponent, ApplicationSettingsService applicationSettingsService)
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


    public class Rmv2ModelNode : GroupNode
    {
        public RmvFile Model { get; set; }

        public Rmv2ModelNode(string name, int lodCount = 4)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex)
                {
                    IsVisible = lodIndex == 0
                };
                AddObject(lodNode);
            }
        }

        public List<Rmv2LodNode> GetLodNodes()
        {
            return Children
                .Where(x => x is Rmv2LodNode)
                .Select(x => x as Rmv2LodNode)
                .ToList();
        }

        public Rmv2MeshNode GetMeshNode(int lod, int modelIndex)
        {
            var lods = GetLodNodes();
            while (lods.Count <= lod)
            {
                Children.Add(new Rmv2LodNode("Test", 12));
                lods = GetLodNodes();
            }

            if (lods[lod].Children.Count <= modelIndex)
                return null;
            return lods[lod].Children[modelIndex] as Rmv2MeshNode;
        }

        public List<Rmv2MeshNode> GetMeshNodes(int lod)
        {
            var lodNodes = GetLodNodes();
            var lodNode = lodNodes[lod];
            var meshes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(lodNode);
            return meshes;
        }

        public List<Rmv2MeshNode> GetMeshesInLod(int lodIndex, bool onlyVisible)
        {
            var lods = GetLodNodes();
            var orderedLods = lods.OrderBy(x => x.LodValue);

            var meshes = orderedLods
               .ElementAt(lodIndex)
               .GetAllModels(onlyVisible);

            return meshes;
        }


        protected Rmv2ModelNode() { }

        public override ISceneNode CreateCopyInstance() => new Rmv2ModelNode();

        public override void CopyInto(ISceneNode tartet)
        {
            var typedTarget = tartet as Rmv2ModelNode;
            typedTarget.Model = Model;
            base.CopyInto(tartet);
        }
    }
}


