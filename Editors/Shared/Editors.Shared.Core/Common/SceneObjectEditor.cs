using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using static Editors.Shared.Core.Services.SkeletonAnimationLookUpHelper;

namespace Editors.Shared.Core.Common
{
    public class SceneObjectEditor
    {
        private readonly ILogger _logger = Logging.Create<SceneObjectEditor>();
        private readonly IWpfGame _mainScene;
        private readonly IServiceProvider _serviceProvider;
        private readonly SceneManager _sceneManager;
        private readonly IPackFileService _packFileService;
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly ComplexMeshLoader _complexMeshLoader;

        public SceneObjectEditor(IWpfGame mainScene,
            IServiceProvider serviceProvider, SceneManager sceneManager, IPackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent, ComplexMeshLoader complexMeshLoader)
        {
            _mainScene = mainScene;
            _serviceProvider = serviceProvider;
            _sceneManager = sceneManager;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
            _complexMeshLoader = complexMeshLoader;
        }

        public SceneObject CreateAsset(string description, Color skeletonColour)
        {
            var instance = _serviceProvider.GetRequiredService<SceneObject>();
            var rootNode = _sceneManager.RootNode;
            var parentNode = rootNode.AddObject(new GroupNode(description));

            // Create skeleton
            var skeletonSceneNode = new SkeletonNode(instance.Skeleton);
            skeletonSceneNode.NodeColour = skeletonColour;
            parentNode.AddObject(skeletonSceneNode);

            var animationPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new GameWorld.Core.Animation.AnimationPlayer(), description);
            instance.Player = animationPlayer;
            instance.ParentNode = parentNode;
            instance.SkeletonSceneNode = skeletonSceneNode;
            instance.Description = description;

            return _mainScene.AddComponent(instance);
        }

        public void SetMesh(SceneObject sceneObject, PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");

            var loadedNode = _complexMeshLoader.Load(file, sceneObject.Player, false);// TODO: Could last arg be true? WOuld be better
            if (loadedNode == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            if (sceneObject.ModelNode != null)
                sceneObject.ParentNode.RemoveObject(sceneObject.ModelNode);
            sceneObject.ModelNode = loadedNode;
            sceneObject.ParentNode.AddObject(loadedNode);

            var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
            var fullSkeletonName = $"animations\\skeletons\\{skeletonName}.anim";
            var skeletonFile = _packFileService.FindFile(fullSkeletonName);
            SetSkeleton(sceneObject, skeletonFile);
            sceneObject.MeshName.Value = file.Name;
            sceneObject.ShowMesh.Value = sceneObject.ShowMesh.Value;
            sceneObject.ShowSkeleton.Value = sceneObject.ShowSkeleton.Value;

            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
                {
                    if (sceneObject.Skeleton != null)
                    {
                        var boneIndex = sceneObject.Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(sceneObject, boneIndex);
                    }
                }
            });

            sceneObject.TriggerMeshChanged();
        }

        public void CopyMeshFromOther(SceneObject assetViewModel, SceneObject other)
        {
            if (assetViewModel.ModelNode != null)
                assetViewModel.ParentNode.RemoveObject(assetViewModel.ModelNode);

            if (other.ModelNode == null)
                return;

            assetViewModel.ModelNode = SceneNodeHelper.CloneNodeAndChildren(other.ModelNode);

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

        public void SetSkeleton(SceneObject assetViewModel, PackFile skeletonPackFile)
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

        public void SetSkeleton(SceneObject assetViewModel, AnimationFile animFile, string skeletonName)
        {
            assetViewModel.SkeletonName.Value = skeletonName;
            assetViewModel.Skeleton = new GameSkeleton(animFile, assetViewModel.Player);

            assetViewModel.AnimationClip = null;
            assetViewModel.SkeletonSceneNode.Skeleton = assetViewModel.Skeleton;
            assetViewModel.Player.SetAnimation(null, assetViewModel.Skeleton);

            assetViewModel.TriggerSkeletonChanged();
        }

        public void SetMetaFile(SceneObject sceneObject, PackFile? metaFile, PackFile? persistantFile)
        {
            sceneObject.MetaData = metaFile;
            sceneObject.PersistMetaData = persistantFile;
            sceneObject.TriggerMetaDataChanged();
        }

        public void SetAnimation(SceneObject assetViewModel, AnimationReference? animationReference)
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

        public void SetAnimationClip(SceneObject assetViewModel, AnimationClip clip, AnimationReference animationReference)
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
