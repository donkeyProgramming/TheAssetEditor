using Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class AssetViewModel : BaseComponent, IAnimationProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;
        public event ValueChangedDelegate<AnimationClip> AnimationChanged;

        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;

        ResourceLibary _resourceLibary;
        ISceneNode _parentNode;
        Color _skeletonColor;
        SkeletonNode _skeletonSceneNode;
        List<Rmv2MeshNode> _meshNodes = new List<Rmv2MeshNode>();

        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set { _isSelectable = value; SetSelectableState(); } } 

        public View3D.Animation.AnimationPlayer Player;
        public SkeletonBoneAnimationResolver SnapToBoneResolver { get; set; }
        public ISceneNode MainNode { get => _parentNode; }

        public string Description { get; set; }

        public bool IsActive => true;
        public GameSkeleton Skeleton { get; set; }
        public AnimationClip AnimationClip { get; set; }

        string _meshName;
        public string MeshName { get => _meshName; set => SetAndNotify(ref _meshName, value); }

        string _skeletonName;
        public string SkeletonName { get => _skeletonName; set => SetAndNotify(ref _skeletonName, value); }

        AnimationReference _animationName;
        public AnimationReference AnimationName { get => _animationName; set => SetAndNotify(ref _animationName, value); }


        bool _showMesh = true;
        public bool ShowMesh { get => _showMesh; set { SetAndNotify(ref _showMesh, value); _meshNodes.ForEach((x) => x.IsVisible = value); } }

        bool _isSkeletonVisible = true;
        public bool IsSkeletonVisible { get => _isSkeletonVisible; set { SetAndNotify(ref _isSkeletonVisible, value); _skeletonSceneNode.IsVisible = value; } }


        public Matrix Offset { get; set; } = Matrix.Identity;

        public AssetViewModel(PackFileService pfs, string description, Color skeletonColour, WpfGame game) : base( game)
        {
            Description = description;
            _pfs = pfs;
            _skeletonColor = skeletonColour;
        }

        public override void Initialize()
        {
            var rootNode = GetComponent<SceneManager>().RootNode;
            _resourceLibary = GetComponent<ResourceLibary>();
            var animComp = GetComponent<AnimationsContainerComponent>();

            _parentNode = rootNode.AddObject(new GroupNode(Description)) as GroupNode;
            Player = animComp.RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), Description);

            // Create skeleton
            _skeletonSceneNode = new SkeletonNode(_resourceLibary.Content, this);
            _skeletonSceneNode.NodeColour = _skeletonColor;
            _parentNode.AddObject(_skeletonSceneNode);
            base.Initialize();
        }

        public void SetMesh(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_pfs, _resourceLibary);
            var outSkeletonName = "";
            var result = loader.Load(file, null, Player, ref outSkeletonName);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            for (int i = 0; i < _meshNodes.Count; i++)
                _meshNodes[i].Parent.RemoveObject(_meshNodes[i]);

            _meshNodes.Clear();
            _meshNodes = Rmv2MeshNodeHelper.GetAllVisibleMeshes(result);

            SetSelectableState();

            for (int i = 0; i < _meshNodes.Count; i++)
                _parentNode.AddObject(_meshNodes[i]);

            var fullSkeletonName = $"animations\\skeletons\\{outSkeletonName}.anim";
            var skeletonFile = _pfs.FindFile(fullSkeletonName);
            SetSkeleton(skeletonFile as PackFile);
            MeshName = file.Name;
        }

        void SetSelectableState()
        {
            foreach (var node in _meshNodes)
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = IsSelectable;
            }
        }

        public void CopyMeshFromOther(AssetViewModel other, bool setSkeleton)
        {
            foreach (var mesh in _meshNodes)
                mesh.Parent.RemoveObject(mesh);
            _meshNodes.Clear();

            foreach (var mesh in other._meshNodes)
                _meshNodes.Add(mesh.Clone() as Rmv2MeshNode);

            foreach (var mesh in _meshNodes)
            {
                mesh.IsVisible = true;
                mesh.IsSelectable = IsSelectable;
                mesh.AnimationPlayer = Player;
                _parentNode.AddObject(mesh);
            }

            var skeletonFile = _pfs.FindFile(other.SkeletonName);
            SetSkeleton(skeletonFile as PackFile);
        }

        public void SetMeshPosition(Matrix transform)
        {
            foreach (var mesh in _meshNodes)
            {
                for (int i = 0; i < mesh.Geometry.VertexCount(); i++)
                    mesh.Geometry.TransformVertex(i, transform);
                mesh.Geometry.RebuildVertexBuffer();
            }
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);
                SetSkeleton(skeleton, newSkeletonName);
            }
            else
            {
                SkeletonName = "";
                Skeleton = null;
                AnimationClip = null;
                Player.SetAnimation(null, Skeleton);
                SkeletonChanged?.Invoke(Skeleton);
            }
        }

        public void SetTransform(Matrix matrix)
        {
            foreach (var node in _meshNodes)
                node.ModelMatrix = matrix;
        }

        public void SetSkeleton(AnimationFile animFile, string skeletonName)
        {
            SetSkeleton(new GameSkeleton(animFile, Player), skeletonName);
        }

        public void SetSkeleton(GameSkeleton gameSkeleton, string skeletonName)
        {
            SkeletonName = skeletonName;
            Skeleton = gameSkeleton;

            AnimationClip = null;
            Player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }

        internal void OnlyShowMeshRelatedToBones(List<int> selectedBoneIds, List<IndexRemapping> remapping, string newSkeletonName)
        {
            var boneArray = selectedBoneIds.ToArray();
            foreach (var meshNode in _meshNodes)
            {
                var geo = meshNode.Geometry as Rmv2Geometry;
                geo.RemoveAllVertexesNotUsedByBones(boneArray);
                geo.UpdateAnimationIndecies(remapping);
                meshNode.MeshModel.ParentSkeletonName = newSkeletonName;
            }
        }

        internal void SelectedBoneIndex(int? boneIndex)
        {
            _skeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        public void SetAnimation(AnimationReference animationReference)
        {
            if (animationReference != null)
            {
                var file = _pfs.FindFile(animationReference.AnimationFile, animationReference.Container) as PackFile;
                AnimationName = animationReference;
                var animation = AnimationFile.Create(file);
                SetAnimationClip(new AnimationClip(animation), animationReference);
            }
            else
            {
                SetAnimationClip(null, null);
            }
        }

        public void SetAnimationClip(AnimationClip clip, AnimationReference animationReference)
        {
            var frame = Player.CurrentFrame;
            AnimationClip = clip;
            AnimationName = animationReference;
            Player.SetAnimation(AnimationClip, Skeleton);
            AnimationChanged?.Invoke(clip);
            Player.CurrentFrame = frame;
        }

        public override void Update(GameTime gameTime)
        {
            if (SnapToBoneResolver != null)
                _parentNode.ModelMatrix = Matrix.Multiply(Offset, SnapToBoneResolver.GetWorldTransform());
            else
                _parentNode.ModelMatrix = Matrix.Multiply(Offset,Matrix.Identity);
        }
    }

    public class AnimatedPropViewModel : NotifyPropertyChangedImpl, IAnimationProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;

        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;

        ResourceLibary _resourceLibary;
        ISceneNode _parentNode;
        SkeletonNode _skeletonSceneNode;
        List<Rmv2MeshNode> _meshNodes = new List<Rmv2MeshNode>();
        View3D.Animation.AnimationPlayer _player;

        public bool IsActive => true;
        public GameSkeleton Skeleton { get; set; }


        string _meshName;
        public string MeshName { get => _meshName; set => SetAndNotify(ref _meshName, value); }

        string _skeletonName;
        public string SkeletonName { get => _skeletonName; set => SetAndNotify(ref _skeletonName, value); }

        string _animationName;
        public string AnimationName { get => _animationName; set => SetAndNotify(ref _animationName, value); }


        bool _showMesh = true;
        public bool ShowMesh { get => _showMesh; set { SetAndNotify(ref _showMesh, value); _meshNodes.ForEach((x) => x.IsVisible = value); } }

        bool _isSkeletonVisible = true;
        public bool IsSkeletonVisible { get => _isSkeletonVisible; set { SetAndNotify(ref _isSkeletonVisible, value); _skeletonSceneNode.IsVisible = value; } }

        public AnimatedPropViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        internal void Initialize(ResourceLibary resourceLib, View3D.Animation.AnimationPlayer animationPlayer, ISceneNode parentNode, Color skeletonColour)
        {
            _resourceLibary = resourceLib;
            _parentNode = parentNode;
            _player = animationPlayer;
            _skeletonSceneNode = new SkeletonNode(_resourceLibary.Content, this);
            _skeletonSceneNode.NodeColour = skeletonColour;
            parentNode.AddObject(_skeletonSceneNode);
        }

        public void SetMesh(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_pfs, _resourceLibary);
            var outSkeletonName = "";
            var result = loader.Load(file, null, _player, ref outSkeletonName);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            result.ForeachNode((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;
            });

            for (int i = 0; i < _meshNodes.Count; i++)
                _meshNodes[i].Parent.RemoveObject(_meshNodes[i]);

            _meshNodes.Clear();
            _meshNodes = Rmv2MeshNodeHelper.GetAllVisibleMeshes(result);

            for (int i = 0; i < _meshNodes.Count; i++)
                _parentNode.AddObject(_meshNodes[i]);

            var fullSkeletonName = $"animations\\skeletons\\{outSkeletonName}.anim";
            var skeletonFile = _pfs.FindFile(fullSkeletonName);
            SetSkeleton(skeletonFile as PackFile);
            MeshName = file.Name;
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName)
                    return;

                SkeletonName = newSkeletonName;
                var skeletonAnimationFile = AnimationFile.Create(skeletonPackFile);
                Skeleton = new GameSkeleton(skeletonAnimationFile, _player);
            }
            else
            {
                SkeletonName = "";
                Skeleton = null;
            }
            _player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }

        internal void OnlyShowMeshRelatedToBones(List<int> selectedBoneIds)
        {
            var boneArray = selectedBoneIds.ToArray();
            foreach (var meshNode in _meshNodes)
            {
                //meshNode.IsVisible = false;
                var geo = meshNode.Geometry as Rmv2Geometry;
                //if (geo.ContainsAnimationBone(boneArray) == false)
                //    meshNode.IsVisible = false;
                //else
                geo.RemoveAllVertexesNotUsedByBones(boneArray);
            }
        }

        internal void SelectedBoneIndex(int? boneIndex)
        {
            _skeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        public void SetAnimation(PackFile file)
        {
            if (file != null)
            {
                var animFile = AnimationFile.Create(file);
                var animClip = new AnimationClip(animFile);
                AnimationName = _pfs.GetFullPath(file);
                _player.SetAnimation(animClip, Skeleton);
                _player.Play();
            }
            else
            {
                AnimationName = "";
                _player.SetAnimation(null, Skeleton);
            }
        }
    }
}
