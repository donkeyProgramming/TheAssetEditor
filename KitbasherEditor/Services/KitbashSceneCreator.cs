using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.ViewModels;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.Services
{
    public class KitbashSceneCreator
    {
        ILogger _logger = Logging.Create<KitbashSceneCreator>();

        public MainEditableNode MainNode { get; private set; }
        public ISceneNode ReferenceMeshNode { get; private set; }

        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        AnimationControllerViewModel _animationView;
        SceneManager _sceneManager;
        IGeometryGraphicsContextFactory _geometryFactory;
        IComponentManager _componentManager;
        ApplicationSettingsService _applicationSettingsService;

        public KitbashSceneCreator(IComponentManager componentManager, PackFileService packFileService, AnimationControllerViewModel animationView, PackFile mainFile, IGeometryGraphicsContextFactory geometryFactory, ApplicationSettingsService applicationSettingsService)
        {
            _componentManager = componentManager;
            _packFileService = packFileService;
            _animationView = animationView;
            _geometryFactory = geometryFactory;
            _applicationSettingsService = applicationSettingsService;

            _resourceLibary = componentManager.GetComponent<ResourceLibary>();
            _sceneManager = componentManager.GetComponent<SceneManager>();

            var skeletonNode = _sceneManager.RootNode.AddObject(new SkeletonNode(componentManager, null) { IsLockable = false });
            MainNode = _sceneManager.RootNode.AddObject(new MainEditableNode(_animationView.GetPlayer(), "Editable Model", skeletonNode, mainFile, packFileService));
            ReferenceMeshNode = _sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false, IsLockable = false });
        }

        public void LoadMainEditableModel(PackFile file)
        {
            var modelFullPath = _packFileService.GetFullPath(file);
            var rmv = ModelFactory.Create().Load(file.DataSource.ReadData());

            MainNode.CreateModelNodesFromFile(rmv, _resourceLibary, _animationView.GetPlayer(), _geometryFactory, modelFullPath, _componentManager, _packFileService, _applicationSettingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes);
            MainNode.SelectedOutputFormat = rmv.Header.Version;

            int meshCount = Math.Min(MainNode.Children.Count, rmv.LodHeaders.Length);
            for(int i = 0; i < meshCount; i++)
            {
                if (MainNode.Children[i] is Rmv2LodNode lodNode)
                    lodNode.CameraDistance = rmv.LodHeaders[i].LodCameraDistance;
            }

            MainNode.SetSkeletonFromName(rmv.Header.SkeletonName);
            _animationView.SetActiveSkeletonFromName(rmv.Header.SkeletonName);
        }

        public void LoadReference(string path)
        {
            _logger.Here().Information($"Loading reference model from path - {path}");

            var refereneceMesh = _packFileService.FindFile(path);
            if (refereneceMesh == null)
            {
                _logger.Here().Error("Unable to find file");
                return;
            }

            LoadReference(refereneceMesh);
        }

        public void LoadModelIntoMainScene(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);

            var lodNodes = new List<Rmv2LodNode>();
            result.ForeachNodeRecursive((node) =>
            {
                if (node is Rmv2LodNode lodNode && lodNode.LodValue == 0)
                    lodNodes.Add(lodNode);
            });

            foreach (var node in lodNodes)
                SceneNodeHelper.MakeNodeEditable(MainNode, node);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);
            ReferenceMeshNode.AddObject(result);
        }

        SceneNode LoadModel(PackFile file)
        {
            SceneLoader loader = new SceneLoader(_resourceLibary, _packFileService, _geometryFactory, _componentManager, _applicationSettingsService);
            var loadedNode = loader.Load(file, null, _animationView.GetPlayer());

            if (loadedNode == null)
            {
                _logger.Here().Error("Unable to load model");
                return null;
            }

            loadedNode.ForeachNodeRecursive((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                {
                    selectable.IsSelectable = false;
                }

                if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
                {
                    if (MainNode.SkeletonNode.Skeleton != null)
                    {
                        int boneIndex = MainNode.SkeletonNode.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(MainNode.SkeletonNode, boneIndex);
                    }
                }
            });

            return loadedNode;
        }
    }
        
}
