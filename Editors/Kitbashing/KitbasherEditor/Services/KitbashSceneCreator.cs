using System.IO;
using Editors.KitbasherEditor.Core;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace Editors.KitbasherEditor.Services
{
    public class KitbashSceneCreator
    {
        private readonly ILogger _logger = Logging.Create<KitbashSceneCreator>();
        private readonly IPackFileService _packFileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly SceneManager _sceneManager;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;
        private readonly GeometrySaveSettings _saveSettings;

        public KitbashSceneCreator(
            ApplicationSettingsService settingsService,
            KitbasherRootScene kitbasherRootScene,
            ComplexMeshLoader complexMeshLoader,
            SceneManager sceneManager,
            IPackFileService packFileService,
            Rmv2ModelNodeLoader rmv2ModelNodeLoader,
            GeometrySaveSettings saveSettings)
        {
            _packFileService = packFileService;
            _settingsService = settingsService;
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

            // Load the opened model
            var modelFullPath = _packFileService.GetFullPath(file);

            WsModelFile? wsModel = null;
            RmvFile rmv;
            if (Path.GetExtension(modelFullPath).ToLower() == ".wsmodel")
            {
                wsModel = new WsModelFile(file);
                var rmvPackFile = _packFileService.FindFile(wsModel.GeometryPath);
                rmv = ModelFactory.Create().Load(rmvPackFile.DataSource.ReadData());
            }
            else
            {
                rmv = ModelFactory.Create().Load(file.DataSource.ReadData());
            }

            var lodNodes = _rmv2ModelNodeLoader.CreateModelNodesFromFile(rmv, modelFullPath, false, wsModel);
            foreach (var lodNode in lodNodes)
            {
                SceneNodeHelper
                    .GetChildrenOfType<Rmv2MeshNode>(lodNode)
                    .ForEach(x=>x.AnimationPlayer = _kitbasherRootScene.Player);
            }
            
            mainNode.Children.Clear();
            foreach(var lodNode in lodNodes)
                mainNode.AddObject(lodNode);

            _kitbasherRootScene.SetSkeletonFromName(rmv.Header.SkeletonName);

            var attachmentPointList = GetAttachmentPointsFromFirstModel(rmv);
            mainNode.SetAttachmentPoints(attachmentPointList, true);

            var fullPath = _packFileService.GetFullPath(file);
            var dirPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(dirPath) == false)
                _saveSettings.OutputName = dirPath + "\\";
            _saveSettings.OutputName+= Path.GetFileNameWithoutExtension(fullPath) + ".rigid_model_v2";
            _saveSettings.InitializeLodSettings(rmv.LodHeaders);
        }

        List<RmvAttachmentPoint> GetAttachmentPointsFromFirstModel(RmvFile file)
        {
            var firstMesh = file.ModelList.FirstOrDefault()?.FirstOrDefault();
            if (firstMesh == null)
                return [];

            if (firstMesh.Material is not WeightedMaterial material)
                return [];

            return material.AttachmentPointParams;
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file, _settingsService.CurrentSettings.OnlyLoadLod0ForReferenceMeshes);

            var referenceMeshNode = _sceneManager.GetNodeByName<GroupNode>(SpecialNodes.ReferenceMeshs);
            referenceMeshNode.AddObject(result!);
        }

        SceneNode? LoadModel(PackFile file, bool onlyLoadRootNode)
        {
            var loadedNode = _complexMeshLoader.Load(file, _kitbasherRootScene.Player, onlyLoadRootNode, false);
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
                    mesh.AnimationPlayer = _kitbasherRootScene.Player;
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
