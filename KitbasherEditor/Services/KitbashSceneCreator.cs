using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.ViewModels;
using MonoGame.Framework.WpfInterop;
using Serilog;
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

        public MainEditableNode EditableMeshNode { get; private set; }
        public ISceneNode ReferenceMeshRoot { get; private set; }

        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        AnimationControllerViewModel _animationView;
        SceneManager _sceneManager;
        IGeometryGraphicsContextFactory _geometryFactory;

        public KitbashSceneCreator(IComponentManager componentManager, PackFileService packFileService, AnimationControllerViewModel animationView, PackFile mainFile, IGeometryGraphicsContextFactory geometryFactory)
        {
            _packFileService = packFileService;
            _resourceLibary = componentManager.GetComponent<ResourceLibary>();
            _animationView = animationView;
            _sceneManager = componentManager.GetComponent<SceneManager>();
            _geometryFactory = geometryFactory;

            var skeletonNode = _sceneManager.RootNode.AddObject(new SkeletonNode(componentManager, animationView) { IsLockable = false });
            EditableMeshNode = _sceneManager.RootNode.AddObject(new MainEditableNode("Editable Model", skeletonNode, mainFile));
            ReferenceMeshRoot = _sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false, IsLockable = false });
        }

        public void LoadMainEditableModel(PackFile file)
        {
            var rmv = ModelFactory.Create().Load(file.DataSource.ReadData());

            EditableMeshNode.CreateModelNodesFromFile(rmv, _resourceLibary, _animationView.Player, _geometryFactory);
            EditableMeshNode.SelectedOutputFormat = rmv.Header.Version;

            for(int i=0; i<EditableMeshNode.Children.Count; i++)
            {
                if (EditableMeshNode.Children[i] is Rmv2LodNode lodNode)
                {
                    lodNode.CameraDistance = rmv.LodHeaders[i].LodCameraDistance;
                }
            }

            _animationView.SetActiveSkeleton(rmv.Header.SkeletonName);
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
                SceneNodeHelper.MakeNodeEditable(EditableMeshNode, node);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");
            var result = LoadModel(file);
            ReferenceMeshRoot.AddObject(result);
        }

        SceneNode LoadModel(PackFile file)
        {
            SceneLoader loader = new SceneLoader(_resourceLibary, _packFileService, _geometryFactory);
            var loadedNode = loader.Load(file, null, _animationView.Player);

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
                    if (EditableMeshNode.Skeleton.AnimationProvider?.Skeleton != null)
                    {
                        int boneIndex = EditableMeshNode.Skeleton.AnimationProvider.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(EditableMeshNode.Skeleton.AnimationProvider, boneIndex);
                    }
                }
            });

            return loadedNode;
        }
    }
        
}
