using AnimationEditor.Common.AnimationPlayer;
using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.DB;
using FileTypes.MetaData;
using FileTypes.MetaData.Instances;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Animation.MetaData;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class AssetViewModel : BaseComponent, ISkeletonProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;
        public event ValueChangedDelegate<AnimationClip> AnimationChanged;
        public event ValueChangedDelegate<AssetViewModel> MeshChanged;
        public event ValueChangedDelegate<AssetViewModel> MetaDataChanged;


        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;
        ResourceLibary _resourceLibary;
        SceneNode _parentNode;
        Color _skeletonColor;
        SkeletonNode _skeletonSceneNode;
        List<Rmv2MeshNode> _meshNodes = new List<Rmv2MeshNode>();

        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set { _isSelectable = value; SetSelectableState(); } } 

        public View3D.Animation.AnimationPlayer Player;
        public List<IMetaDataInstance> MetaDataItems { get; set; } = new List<IMetaDataInstance>();

        public SceneNode MainNode { get => _parentNode; }

        public string Description { get; set; }

        public bool IsActive => true;
        public GameSkeleton Skeleton { get; private set; }
        public AnimationClip AnimationClip { get; private set; }
        public PackFile MetaData { get; private set; }
        public PackFile PersistMetaData { get; private set; }
        public Matrix Offset { get; set; } = Matrix.Identity;


        // --- UI elements
        public NotifyAttr<string> MeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<AnimationReference> AnimationName { get; set; } = new NotifyAttr<AnimationReference>(null);

        public NotifyAttr<bool> ShowMesh { get; set; }
        public NotifyAttr<bool> ShowSkeleton { get; set; }


        public AssetViewModel(PackFileService pfs, string description, Color skeletonColour, WpfGame game) : base( game)
        {
            Description = description;
            _pfs = pfs;
            _skeletonColor = skeletonColour;

            ShowMesh = new NotifyAttr<bool>(true, (x) => _meshNodes.ForEach((x) => x.IsVisible = ShowMesh.Value));
            ShowSkeleton = new NotifyAttr<bool>(true, (x) => _skeletonSceneNode.IsVisible = ShowSkeleton.Value);
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

            SceneLoader loader = new SceneLoader(_resourceLibary);
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
            SetSkeleton(skeletonFile);
            MeshName.Value = file.Name;
            ShowMesh.Value = ShowMesh.Value;
            ShowSkeleton.Value = ShowSkeleton.Value;

            MeshChanged?.Invoke(this);
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

        public void CopyMeshFromOther(AssetViewModel other)
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

            var skeletonFile = _pfs.FindFile(other.SkeletonName.Value);
            SetSkeleton(skeletonFile);

            ShowMesh.Value = ShowMesh.Value;
            ShowSkeleton.Value = ShowSkeleton.Value;

            MeshChanged?.Invoke(this);
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
                if (newSkeletonName == SkeletonName.Value)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);
                SetSkeleton(skeleton, newSkeletonName);
            }
            else
            {
                SkeletonName.Value = "";
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
            SkeletonName.Value = skeletonName;
            Skeleton = new GameSkeleton(animFile, Player);

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
                AnimationName.Value = animationReference;
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



        public override void Update(GameTime gameTime)
        {
            _parentNode.ModelMatrix = Matrix.Multiply(Offset,Matrix.Identity);

            var p = Player.CurrentFrame;
            foreach (var item in MetaDataItems)
                item.Update(p);
        }

        
    }
}
