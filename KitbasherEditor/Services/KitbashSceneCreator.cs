using System.Collections.Generic;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.ViewModels;
using Serilog;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.Services
{
    public class KitbashSceneCreator
    {
        private readonly ILogger _logger = Logging.Create<KitbashSceneCreator>();

        private MainEditableNode MainNode { get; set; }
        public ISceneNode ReferenceMeshNode { get; private set; }

        private readonly PackFileService _packFileService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly ResourceLibary _resourceLibary;
        private readonly SceneManager _sceneManager;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;

        public KitbashSceneCreator(
            KitbasherRootScene kitbasherRootScene,
            ComplexMeshLoader complexMeshLoader,
            ResourceLibary resourceLibary,
            SceneManager sceneManager,
            PackFileService packFileService,
            Rmv2ModelNodeLoader rmv2ModelNodeLoader)
        {
            _packFileService = packFileService;
            _kitbasherRootScene = kitbasherRootScene;
            _complexMeshLoader = complexMeshLoader;
            _resourceLibary = resourceLibary;
            _sceneManager = sceneManager;
            _rmv2ModelNodeLoader = rmv2ModelNodeLoader;
        }

        public void Create()
        {
            var skeletonNode = _sceneManager.RootNode.AddObject(new SkeletonNode(_resourceLibary, null) { IsLockable = false });
            MainNode = _sceneManager.RootNode.AddObject(new MainEditableNode(SpecialNodes.EditableModel, skeletonNode, _packFileService));
            ReferenceMeshNode = _sceneManager.RootNode.AddObject(new GroupNode(SpecialNodes.ReferenceMeshs) { IsEditable = false, IsLockable = false });
        }

        public void LoadMainEditableModel(PackFile file)
        {
            var modelFullPath = _packFileService.GetFullPath(file);
            var rmv = ModelFactory.Create().Load(file.DataSource.ReadData());

            _rmv2ModelNodeLoader.CreateModelNodesFromFile(MainNode, rmv, _kitbasherRootScene.Player, modelFullPath);
            _kitbasherRootScene.SetSkeletonFromName(rmv.Header.SkeletonName);
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
            var loadedNode = _complexMeshLoader.Load(file, _kitbasherRootScene.Player);
            if (loadedNode == null)
            {
                _logger.Here().Error("Unable to load model");
                return null;
            }

            loadedNode.ForeachNodeRecursive((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;

                if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
                {
                    if (_kitbasherRootScene.Skeleton != null)
                    {
                        int boneIndex = _kitbasherRootScene.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(_kitbasherRootScene, boneIndex);
                    }
                }
            });

            return loadedNode;
        }
    }
}
