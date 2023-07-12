using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class AssetViewModelBuilder
    {
        ILogger _logger = Logging.Create<AssetViewModelBuilder>();
        private readonly GameWorld _mainScene;
        private readonly IServiceProvider _serviceProvider;
        private readonly ResourceLibary _resourceLibary;
        private readonly SceneManager _sceneManager;
        private readonly PackFileService _packFileService;
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly ComplexMeshLoader _complexMeshLoader;

        public AssetViewModelBuilder(GameWorld mainScene,
            IServiceProvider serviceProvider, ResourceLibary resourceLibary, SceneManager sceneManager, PackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent, ComplexMeshLoader complexMeshLoader)
        {
            _mainScene = mainScene;
            _serviceProvider = serviceProvider;
            _resourceLibary = resourceLibary;
            _sceneManager = sceneManager;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
            _complexMeshLoader = complexMeshLoader;
        }

        public AssetViewModel CreateAsset(string description, Color skeletonColour)
        {
            var instance = _serviceProvider.GetRequiredService<AssetViewModel>();

            var rootNode = _sceneManager.RootNode;
            var parentNode = rootNode.AddObject(new GroupNode(description));


            // Create skeleton
            var skeletonSceneNode = new SkeletonNode(_resourceLibary, instance.Skeleton);
            skeletonSceneNode.NodeColour = skeletonColour;
            parentNode.AddObject(skeletonSceneNode);

            var animationPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), description);
            instance.Player = animationPlayer;
            instance.ParentNode = parentNode;
            instance.SkeletonSceneNode = skeletonSceneNode;

            return _mainScene.AddComponent(instance);

        }

        public void SetMesh(AssetViewModel assetViewModel, PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");

            var loadedNode = _complexMeshLoader.Load(file, assetViewModel.Player);
            if (loadedNode == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            if (assetViewModel.ModelNode != null)
                assetViewModel.ParentNode.RemoveObject(assetViewModel.ModelNode);
            assetViewModel.ModelNode = loadedNode;
            assetViewModel.ParentNode.AddObject(loadedNode);

            var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
            var fullSkeletonName = $"animations\\skeletons\\{skeletonName}.anim";
            var skeletonFile = _packFileService.FindFile(fullSkeletonName);
            SetSkeleton(assetViewModel, skeletonFile);
            assetViewModel.MeshName.Value = file.Name;
            assetViewModel.ShowMesh.Value = assetViewModel.ShowMesh.Value;
            assetViewModel.ShowSkeleton.Value = assetViewModel.ShowSkeleton.Value;

            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
                {
                    if (assetViewModel.Skeleton != null)
                    {
                        int boneIndex = assetViewModel.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(assetViewModel, boneIndex);
                    }
                }
            });

            assetViewModel.TriggerMeshChanged();
        }

        public void CopyMeshFromOther(AssetViewModel assetViewModel, AssetViewModel other)
        {
            if (assetViewModel.ModelNode != null)
                assetViewModel.ParentNode.RemoveObject(assetViewModel.ModelNode);

            if (other.ModelNode == null)
                return;

            assetViewModel.ModelNode = SceneNodeHelper.DeepCopy(other.ModelNode);

            var cloneMeshes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(assetViewModel.ModelNode);
            foreach (var mesh in cloneMeshes)
                mesh.AnimationPlayer = assetViewModel.Player;

            assetViewModel.ParentNode.AddObject(assetViewModel.ModelNode);
            var skeletonFile = _packFileService.FindFile(other.SkeletonName.Value);
            SetSkeleton(assetViewModel, skeletonFile);

            assetViewModel.ShowMesh.Value = assetViewModel.ShowMesh.Value;
            assetViewModel.ShowSkeleton.Value = assetViewModel.ShowSkeleton.Value;
            assetViewModel.SetMeshVisability(assetViewModel.ShowMesh.Value);

            assetViewModel.TriggerMeshChanged();
        }

        public void SetSkeleton(AssetViewModel assetViewModel, PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _packFileService.GetFullPath(skeletonPackFile);
                if (newSkeletonName == assetViewModel.SkeletonName.Value)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);

                assetViewModel.SkeletonName.Value = newSkeletonName;
                assetViewModel.Skeleton = new GameSkeleton(skeleton, assetViewModel.Player);

                assetViewModel.AnimationClip = null;
                assetViewModel.SkeletonSceneNode.Skeleton = assetViewModel.Skeleton;
                assetViewModel.Player.SetAnimation(null, assetViewModel.Skeleton);
            }
            else
            {
                if (assetViewModel.Skeleton == null)
                    return;
                assetViewModel.SkeletonName.Value = "";
                assetViewModel.Skeleton = null;
                assetViewModel.AnimationClip = null;
                assetViewModel.Player.SetAnimation(null, assetViewModel.Skeleton); ;
            }

            assetViewModel.TriggerSkeletonChanged();
        }

        public void SetSkeleton(AssetViewModel assetViewModel, AnimationFile animFile, string skeletonName)
        {
            assetViewModel.SkeletonName.Value = skeletonName;
            assetViewModel.Skeleton = new GameSkeleton(animFile, assetViewModel.Player);

            assetViewModel.AnimationClip = null;
            assetViewModel.SkeletonSceneNode.Skeleton = assetViewModel.Skeleton;
            assetViewModel.Player.SetAnimation(null, assetViewModel.Skeleton);

            assetViewModel.TriggerSkeletonChanged();
        }

        public void SetMetaFile(AssetViewModel assetViewModel, PackFile metaFile, PackFile persistantFile)
        {
            assetViewModel.MetaData = metaFile;
            assetViewModel.PersistMetaData = persistantFile;
            assetViewModel.TriggerMetaDataChanged();
        }

        public void SetAnimation(AssetViewModel assetViewModel, AnimationReference animationReference)
        {
            if (animationReference != null)
            {
                var file = _packFileService.FindFile(animationReference.AnimationFile, animationReference.Container);
                assetViewModel.AnimationName.Value = animationReference;
                var animation = AnimationFile.Create(file);
                SetAnimationClip(assetViewModel, new AnimationClip(animation, assetViewModel.Skeleton), animationReference);
            }
            else
            {
                SetAnimationClip(assetViewModel, null, null);
            }
        }

        public void SetAnimationClip(AssetViewModel assetViewModel, AnimationClip clip, AnimationReference animationReference)
        {
            if (assetViewModel.AnimationClip == null && clip == null && animationReference == null)
                return;
            var frame = assetViewModel.Player.CurrentFrame;
            assetViewModel.AnimationClip = clip;
            assetViewModel.AnimationName.Value = animationReference;
            assetViewModel.Player.SetAnimation(assetViewModel.AnimationClip, assetViewModel.Skeleton);
            assetViewModel.TriggerAnimationChanged();
            assetViewModel.Player.CurrentFrame = frame;
        }


    }
}
