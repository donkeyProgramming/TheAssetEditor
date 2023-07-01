using Assimp.Unmanaged;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using View3D.Animation;
using View3D.Animation.MetaData;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.Editors.AnimationPack.Converters.AnimationBinFileToXmlConverter;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{



    public class AssetViewModelBuilder
    {
        ILogger _logger = Logging.Create<AssetViewModelBuilder>();
        private readonly IGeometryGraphicsContextFactory _geometryGraphicsContextFactory;
        private readonly MainScene _mainScene;
        private readonly IServiceProvider _serviceProvider;
        private readonly ResourceLibary _resourceLibary;
        private readonly SceneManager _sceneManager;
        private readonly PackFileService _packFileService;
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly SceneLoader _sceneLoader;

        public AssetViewModelBuilder(IGeometryGraphicsContextFactory geometryGraphicsContextFactory, MainScene mainScene, 
            IServiceProvider serviceProvider, ResourceLibary resourceLibary, SceneManager sceneManager, PackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent, ApplicationSettingsService applicationSettingsService, SceneLoader sceneLoader)
        {
            _geometryGraphicsContextFactory = geometryGraphicsContextFactory;
            _mainScene = mainScene;
            _serviceProvider = serviceProvider;
            _resourceLibary = resourceLibary;
            _sceneManager = sceneManager;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
            _applicationSettingsService = applicationSettingsService;
            _sceneLoader = sceneLoader;
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

            var loadedNode = _sceneLoader.Load(file, null, assetViewModel.Player);
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
                assetViewModel.Player.SetAnimation(null, assetViewModel.Skeleton);;
            }

            assetViewModel.TriggerSkeletonChanged();
        }
    }



    public class AssetViewModel : BaseComponent, ISkeletonProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;
        public event ValueChangedDelegate<AnimationClip> AnimationChanged;
        public event ValueChangedDelegate<AssetViewModel> MeshChanged;
        public event ValueChangedDelegate<AssetViewModel> MetaDataChanged;

        public void TriggerMeshChanged() => MeshChanged?.Invoke(this);
        public void TriggerSkeletonChanged() => SkeletonChanged?.Invoke(this.Skeleton);

        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;
        ResourceLibary _resourceLibary;
        public SceneNode ParentNode;
        public SkeletonNode SkeletonSceneNode;
        public ISceneNode ModelNode;
        IComponentManager _componentManager;
        ApplicationSettingsService _applicationSettingsService;

        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set { _isSelectable = value; SetSelectableState(); } } 

        public View3D.Animation.AnimationPlayer Player;
        public List<IMetaDataInstance> MetaDataItems { get; set; } = new List<IMetaDataInstance>();

        public SceneNode MainNode { get => ParentNode; }

        public string Description { get; set; }

        public bool IsActive => true;
        public GameSkeleton Skeleton { get; set; }
        public AnimationClip AnimationClip { get; set; }
        public PackFile MetaData { get; private set; }
        public PackFile PersistMetaData { get; private set; }
        public Matrix Offset { get; set; } = Matrix.Identity;


        // --- UI elements
        public NotifyAttr<string> MeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<AnimationReference> AnimationName { get; set; } = new NotifyAttr<AnimationReference>(null);

        public NotifyAttr<bool> ShowMesh { get; set; }
        public NotifyAttr<bool> ShowSkeleton { get; set; }


        public AssetViewModel(PackFileService pfs, ComponentManagerResolver componentManagerResolver, ApplicationSettingsService applicationSettingsService)  : base()
        {
                 
            _pfs = pfs;

            _componentManager = componentManagerResolver.ComponentManager;
            _applicationSettingsService = applicationSettingsService;

            ShowMesh = new NotifyAttr<bool>(true, (x) => SetMeshVisability(x));
            ShowSkeleton = new NotifyAttr<bool>(true, (x) => SkeletonSceneNode.IsVisible = ShowSkeleton.Value);
        }

        public override void Initialize()
        {
           //var rootNode = _componentManager.GetComponent<SceneManager>().RootNode;
           //_resourceLibary = _componentManager.GetComponent<ResourceLibary>();
           //var animComp = _componentManager.GetComponent<AnimationsContainerComponent>();
           //
           //ParentNode = rootNode.AddObject(new GroupNode(Description));
           //Player = animComp.RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), Description);
           //
           //// Create skeleton
           //SkeletonSceneNode = new SkeletonNode(_resourceLibary, Skeleton);
           //SkeletonSceneNode.NodeColour = _skeletonColor;
           //ParentNode.AddObject(SkeletonSceneNode);

            base.Initialize();
        }

        void SetMeshVisability(bool value)
        {
            if (ModelNode == null)
                return;
            ModelNode.IsVisible = value;
        }

        public void SetMesh(PackFile file)
        {
            //_logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");
            //
            //var graphics = _componentManager.GetComponent<DeviceResolverComponent>();
            //SceneLoader loader = new SceneLoader(_resourceLibary, _pfs, GeometryGraphicsContextFactory.CreateInstance(graphics.Device), _componentManager, _applicationSettingsService);
            //var loadedNode = loader.Load(file, null, Player);
            //if (loadedNode == null)
            //{
            //    _logger.Here().Error("Unable to load model");
            //    return;
            //}
            //
            //if (ModelNode != null)
            //    ParentNode.RemoveObject(ModelNode);
            //ModelNode = loadedNode;
            //ParentNode.AddObject(loadedNode);
            //
            //var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
            //var fullSkeletonName = $"animations\\skeletons\\{skeletonName}.anim";
            //var skeletonFile = _pfs.FindFile(fullSkeletonName);
            //SetSkeleton(skeletonFile);
            //MeshName.Value = file.Name;
            //ShowMesh.Value = ShowMesh.Value;
            //ShowSkeleton.Value = ShowSkeleton.Value;
            //
            //loadedNode.ForeachNodeRecursive((node) =>
            //{
            //    if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
            //    {
            //        if (Skeleton != null)
            //        {
            //            int boneIndex = Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
            //            mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(this, boneIndex);
            //        }
            //    }
            //});
            //
            //MeshChanged?.Invoke(this);
        }

        void SetSelectableState()
        {
            if (ModelNode == null)
                return;
            ModelNode.ForeachNodeRecursive((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = IsSelectable;
            });
        }

        public void CopyMeshFromOther(AssetViewModel other)
        {
            if (ModelNode != null)
                ParentNode.RemoveObject(ModelNode);

            if (other.ModelNode == null)
                return;

            ModelNode = SceneNodeHelper.DeepCopy(other.ModelNode);

            var cloneMeshes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(ModelNode);
            foreach (var mesh in cloneMeshes)
                mesh.AnimationPlayer = Player;

            ParentNode.AddObject(ModelNode);
            var skeletonFile = _pfs.FindFile(other.SkeletonName.Value);
            SetSkeleton(skeletonFile);
            
            ShowMesh.Value = ShowMesh.Value;
            ShowSkeleton.Value = ShowSkeleton.Value;
            SetMeshVisability(ShowMesh.Value);

            MeshChanged?.Invoke(this);
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName.Value)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);
                SetSkeleton(skeleton, newSkeletonName);
            }
            else
            {
                if (Skeleton == null)
                    return;
                SkeletonName.Value = "";
                Skeleton = null;
                AnimationClip = null;
                Player.SetAnimation(null, Skeleton);
                SkeletonChanged?.Invoke(Skeleton);
            }
        }

        public void SetTransform(Matrix matrix)
        {
            if(ModelNode != null)
                ModelNode.ModelMatrix = matrix;
        }

        public void SetSkeleton(AnimationFile animFile, string skeletonName)
        {
            SkeletonName.Value = skeletonName;
            Skeleton = new GameSkeleton(animFile, Player);
         
            AnimationClip = null;
            SkeletonSceneNode.Skeleton = Skeleton;
            Player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }


        internal void SelectedBoneIndex(int? boneIndex)
        {
            SkeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        internal void SelectedBoneScale(float scaleMult)
        {
            SkeletonSceneNode.SelectedBoneScaleMult = scaleMult;
        }


        public void SetAnimation(AnimationReference animationReference)
        {
            if (animationReference != null)
            {
                var file = _pfs.FindFile(animationReference.AnimationFile, animationReference.Container) ;
                AnimationName.Value = animationReference;
                var animation = AnimationFile.Create(file);
                SetAnimationClip(new AnimationClip(animation, Skeleton), animationReference);
            }
            else
            {
                SetAnimationClip(null, null);
            }
        }

        public void SetAnimationClip(AnimationClip clip, AnimationReference animationReference)
        {
            if (AnimationClip == null && clip == null && animationReference == null)
                return;
            var frame = Player.CurrentFrame;
            AnimationClip = clip;
            AnimationName.Value = animationReference;
            Player.SetAnimation(AnimationClip, Skeleton);
            AnimationChanged?.Invoke(clip);
            Player.CurrentFrame = frame;
        }

        public void SetMetaFile(PackFile metaFile, PackFile persistantFile)
        {
            MetaData = metaFile;
            PersistMetaData = persistantFile;
            MetaDataChanged?.Invoke(this);
        }

        public void ReApplyMeta()
        {
            MetaDataChanged?.Invoke(this);
        }

        public override void Update(GameTime gameTime)
        {
            ParentNode.ModelMatrix = Matrix.Multiply(Offset,Matrix.Identity);

            var p = Player.CurrentFrame;
            foreach (var item in MetaDataItems)
                item.Update(p);
        }

        
    }
}
