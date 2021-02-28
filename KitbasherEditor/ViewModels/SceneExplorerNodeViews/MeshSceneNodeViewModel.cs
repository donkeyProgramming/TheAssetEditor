using Common;
using CommonControls.MathViews;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class MeshSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        public MeshSceneNodeViewModel_General General { get; set; }
        public MeshSceneNodeViewModel_Animation Animation { get; set; }

        Rmv2MeshNode _meshNode;
        public MeshSceneNodeViewModel(Rmv2MeshNode node, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = node;
            General = new MeshSceneNodeViewModel_General(_meshNode);
            Animation = new MeshSceneNodeViewModel_Animation(_meshNode, animLookUp);
        }
    }

    public class MeshSceneNodeViewModel_General : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;

        string _modelName;
        public string ModelName { get { return _modelName; } set { SetAndNotify(ref _modelName, value); } }

        // Vertex type
        VertexFormat _vertexType;
        public VertexFormat VertexType { get { return _vertexType; } set { SetAndNotify(ref _vertexType, value); } }
        public IEnumerable<VertexFormat> PossibleVertexTypes { get; set; }

        public int VertexCount { get => _meshNode.MeshModel.Mesh.VertexList.Length; }
        public int IndexCount { get => _meshNode.MeshModel.Mesh.IndexList.Length; }


        bool _drawPivotPoint = false;
        public bool DrawPivotPoint { get { return _drawPivotPoint; } set { SetAndNotify(ref _drawPivotPoint, value); } }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public MeshSceneNodeViewModel_General(Rmv2MeshNode node)
        {
            _meshNode = node;

            ModelName = _meshNode.MeshModel.Header.ModelName;

            PossibleVertexTypes = Enum.GetValues(typeof(VertexFormat)).Cast<VertexFormat>();
            VertexType = _meshNode.MeshModel.Header.VertextType;

            Pivot = new Vector3ViewModel(_meshNode.MeshModel.Header.Transform.Pivot.X, _meshNode.MeshModel.Header.Transform.Pivot.Y, _meshNode.MeshModel.Header.Transform.Pivot.Z);
        }
    }

    public class MeshSceneNodeViewModel_Animation : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;

        string _skeletonName;
        public string SkeletonName { get { return _skeletonName; } set { SetAndNotify(ref _skeletonName, value); } }
        public ICommand UseParentSkeletonCommand { get; set; }

        int _linkDirectlyToBoneIndex;
        public int LinkDirectlyToBoneIndex { get { return _linkDirectlyToBoneIndex; } set { SetAndNotify(ref _linkDirectlyToBoneIndex, value); } }


        List<RmvAttachmentPoint> _attachemntPoints;
        public List<RmvAttachmentPoint> AttachmentPoints { get { return _attachemntPoints; } set { SetAndNotify(ref _attachemntPoints, value); } }
        public ICommand UseParentAttachmentPointsCommand { get; set; }

        List<AnimatedBone> _animatedBones;
        public List<AnimatedBone> AnimatedBones { get { return _animatedBones; } set { SetAndNotify(ref _animatedBones, value); } }
        public ICommand OpenBoneRemappingToolCommand { get; set; }

        public MeshSceneNodeViewModel_Animation(Rmv2MeshNode meshNode, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = meshNode;

            SkeletonName = _meshNode.MeshModel.ParentSkeletonName;
            UseParentSkeletonCommand = new RelayCommand(UseParentSkeleton);
            LinkDirectlyToBoneIndex = _meshNode.MeshModel.Header.LinkDirectlyToBoneIndex;
            AttachmentPoints = _meshNode.MeshModel.AttachmentPoints.OrderBy(x => x.BoneIndex).ToList();
            UseParentAttachmentPointsCommand = new RelayCommand(UseParentAttachmentPoints);

            var skeletonFile = animLookUp.GetSkeletonFileFromName(SkeletonName);
            var bones = _meshNode.MeshModel.UsedAnimationBones();
            AnimatedBones = bones.Select(x => new AnimatedBone() { BoneIndex = x, Name = skeletonFile.Bones[x].Name }).OrderBy(x=>x.BoneIndex).ToList();
            OpenBoneRemappingToolCommand = new RelayCommand(OpenBoneRemappingTool);
        }


        void UseParentSkeleton() { }

        void UseParentAttachmentPoints() { }

        void OpenBoneRemappingTool() { }
    }

    public class AnimatedBone
    {
        string _name;
        public string Name { get { return _name; } set { _name = value; } }

        int _boneIndex;
        public int BoneIndex { get { return _boneIndex; } set { _boneIndex = value; } }
    }
}

