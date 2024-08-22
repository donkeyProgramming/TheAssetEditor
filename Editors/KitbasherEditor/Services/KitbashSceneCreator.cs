using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Utility;
using KitbasherEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using static Shared.GameFormats.WWise.Hirc.Shared.AkDecisionTree;

namespace Editors.KitbasherEditor.Services
{
    public class KitbashSceneCreator
    {
        private readonly ILogger _logger = Logging.Create<KitbashSceneCreator>();
        private readonly PackFileService _packFileService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly SceneManager _sceneManager;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;
        private readonly GeometrySaveSettings _saveSettings;

        public KitbashSceneCreator(
            KitbasherRootScene kitbasherRootScene,
            ComplexMeshLoader complexMeshLoader,
            SceneManager sceneManager,
            PackFileService packFileService,
            Rmv2ModelNodeLoader rmv2ModelNodeLoader,
            GeometrySaveSettings saveSettings)
        {
            _packFileService = packFileService;
            _kitbasherRootScene = kitbasherRootScene;
            _complexMeshLoader = complexMeshLoader;
            _sceneManager = sceneManager;
            _rmv2ModelNodeLoader = rmv2ModelNodeLoader;
            _saveSettings = saveSettings;
        }

        public void CreateFromPackFile(PackFile file)
        {
            var skeletonNode = _sceneManager.RootNode.AddObject(new SkeletonNode(null) { IsLockable = false });
            var mainNode = _sceneManager.RootNode.AddObject(new MainEditableNode(SpecialNodes.EditableModel, skeletonNode, _packFileService));
            _ = _sceneManager.RootNode.AddObject(new GroupNode(SpecialNodes.ReferenceMeshs) { IsEditable = false, IsLockable = false });

            var modelFullPath = _packFileService.GetFullPath(file);
            var rmv = ModelFactory.Create().Load(file.DataSource.ReadData());

            _rmv2ModelNodeLoader.CreateModelNodesFromFile(mainNode, rmv, _kitbasherRootScene.Player, modelFullPath);
            _kitbasherRootScene.SetSkeletonFromName(rmv.Header.SkeletonName);

            var fullPath = _packFileService.GetFullPath(file);
            _saveSettings.OutputName = fullPath;
            var lodHeaders = mainNode.Model.LodHeaders;
            _saveSettings.InitializeLodSettings(lodHeaders);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);

            var referenceMeshNode = _sceneManager.GetNodeByName<GroupNode>(SpecialNodes.ReferenceMeshs);
            referenceMeshNode.AddObject(result!);
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
