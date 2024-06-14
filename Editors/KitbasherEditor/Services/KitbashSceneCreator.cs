using System.Diagnostics.CodeAnalysis;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using KitbasherEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;

namespace KitbasherEditor.Services
{
    public class KitbashSceneCreator
    {
        private readonly ILogger _logger = Logging.Create<KitbashSceneCreator>();

        private MainEditableNode? _mainNode;
        [AllowNull]public ISceneNode ReferenceMeshNode { get; private set; }

        private readonly PackFileService _packFileService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly SceneManager _sceneManager;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;

        public KitbashSceneCreator(
            KitbasherRootScene kitbasherRootScene,
            ComplexMeshLoader complexMeshLoader,
            ResourceLibrary resourceLibrary,
            SceneManager sceneManager,
            PackFileService packFileService,
            Rmv2ModelNodeLoader rmv2ModelNodeLoader)
        {
            _packFileService = packFileService;
            _kitbasherRootScene = kitbasherRootScene;
            _complexMeshLoader = complexMeshLoader;
            _resourceLibrary = resourceLibrary;
            _sceneManager = sceneManager;
            _rmv2ModelNodeLoader = rmv2ModelNodeLoader;
        }

        public void Create()
        {
            var skeletonNode = _sceneManager.RootNode.AddObject(new SkeletonNode(_resourceLibrary, null) { IsLockable = false });
            _mainNode = _sceneManager.RootNode.AddObject(new MainEditableNode(SpecialNodes.EditableModel, skeletonNode, _packFileService));
            ReferenceMeshNode = _sceneManager.RootNode.AddObject(new GroupNode(SpecialNodes.ReferenceMeshs) { IsEditable = false, IsLockable = false });
        }

        public void LoadMainEditableModel(PackFile file)
        {
            var modelFullPath = _packFileService.GetFullPath(file);
            var rmv = ModelFactory.Create().Load(file.DataSource.ReadData());

            _rmv2ModelNodeLoader.CreateModelNodesFromFile(_mainNode, rmv, _kitbasherRootScene.Player, modelFullPath);
            _kitbasherRootScene.SetSkeletonFromName(rmv.Header.SkeletonName);
        }

        public void LoadModelIntoMainScene(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);
            if (result == null)
                throw new Exception($"Unable to load model - {_packFileService.GetFullPath(file)}");

            var lodNodes = new List<Rmv2LodNode>();
            result.ForeachNodeRecursive((node) =>
            {
                if (node is Rmv2LodNode lodNode && lodNode.LodValue == 0)
                    lodNodes.Add(lodNode);
            });

            foreach (var node in lodNodes)
                SceneNodeHelper.MakeNodeEditable(_mainNode, node);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);
            ReferenceMeshNode.AddObject(result);
        }

        SceneNode? LoadModel(PackFile file)
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
                        var boneIndex = _kitbasherRootScene.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(_kitbasherRootScene, boneIndex);
                    }
                }
            });

            return loadedNode;
        }
    }
}
