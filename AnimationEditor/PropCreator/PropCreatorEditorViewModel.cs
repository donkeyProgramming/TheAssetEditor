using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Services;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using View3D.Animation;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace AnimationEditor.PropCreator
{
    public class PropCreatorEditorViewModel : NotifyPropertyChangedImpl
    {
        public AssetViewModel MainAsset { get; set; }
        public AssetViewModel ReferenceAsset { get; set; }


        public ICommand PreviewCommand { get; set; }

        ObservableCollection<SkeletonBoneNode> _startBoneList;
         public ObservableCollection<SkeletonBoneNode> StartBoneList
         {
             get { return _startBoneList; }
             set { SetAndNotify(ref _startBoneList, value); }
         }

        SkeletonBoneNode _selectedStartBone;
      public SkeletonBoneNode SelectedStartBone
        {
          get { return _selectedStartBone; }
          set { SetAndNotify(ref _selectedStartBone, value);  StartBoneSelected(value); }
      }



        ObservableCollection<SkeletonBoneNode> _endBoneList;
        public ObservableCollection<SkeletonBoneNode> EndBoneList
        {
            get { return _endBoneList; }
            set { SetAndNotify(ref _endBoneList, value); }
        }

        SkeletonBoneNode _selectedEndBone;
        public SkeletonBoneNode SelectedEndBone
        {
            get => _selectedEndBone;
            set  => SetAndNotify(ref _selectedEndBone, value);  
        }


        
        bool _fitAnimation = true;
        public bool FitAnimation
        {
            get { return _fitAnimation; }
            set { SetAndNotify(ref _fitAnimation, value); }
        }

        ObservableCollection<SkeletonBoneNode> _referenceBoneList;
        public ObservableCollection<SkeletonBoneNode> ReferenceBoneList
        {
            get { return _referenceBoneList; }
            set { SetAndNotify(ref _referenceBoneList, value); }
        }

        SkeletonBoneNode _selectedRefBone;
        public SkeletonBoneNode SelectedRefBone
        {
            get { return _selectedRefBone; }
            set { SetAndNotify(ref _selectedRefBone, value); }
        }

        bool _snapToRefBone = true;
        public bool SnapToRefBone
        {
            get { return _snapToRefBone; }
            set { SetAndNotify(ref _snapToRefBone, value);  }
        }

        bool _specifyChildBone = false;
        public bool SpecifyChildBone
        {
            get { return _specifyChildBone; }
            set { SetAndNotify(ref _specifyChildBone, value); if (value == false) SelectedEndBone = null; }
        }

        

        AssetViewModel _data;
        public AssetViewModel Data { get => _data; set => SetAndNotify(ref _data, value); }


        public PropCreatorEditorViewModel(AssetViewModel data, AssetViewModel mainAsset, AssetViewModel refAsset)
        {
            Data = data;
            MainAsset = mainAsset;
            ReferenceAsset = refAsset;

            MainAsset.SkeletonChanged += MainAsset_SkeletonChanged;
            ReferenceAsset.SkeletonChanged += RefAsset_SkeletonChanged;

            PreviewCommand = new RelayCommand(Preview);

            RefAsset_SkeletonChanged(ReferenceAsset.Skeleton);
            MainAsset_SkeletonChanged(MainAsset.Skeleton);
        }

        private void RefAsset_SkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ReferenceBoneList = null;
                return;
            }

            ReferenceBoneList = SkeletonHelper.CreateFlatSkeletonList(newValue);
        }

        private void MainAsset_SkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                StartBoneList = null;
                return;
            }

            StartBoneList = SkeletonHelper.CreateFlatSkeletonList(newValue);
            EndBoneList = null;
            SelectedEndBone = null;
        }

        private void StartBoneSelected(SkeletonBoneNode value)
        {
            if (MainAsset.Skeleton == null)
                return;

            if (value == null)
            {
                EndBoneList = null;
                return;
            }

            EndBoneList = SkeletonHelper.CreateFlatSkeletonList(MainAsset.Skeleton, value.BoneIndex);
        }


        void Preview()
        {
            if (SelectedStartBone == null || (SpecifyChildBone == true && SelectedEndBone == null))
                return;

            var boneEndIndex = SelectedStartBone == null ? -1 : SelectedStartBone.BoneIndex;
            var boneList = SkeletonHelper.CreateFlatSkeletonList(MainAsset.Skeleton, boneEndIndex);
            boneList.Insert(0, SelectedStartBone);
            var selectedBoneIds = boneList.Select(x => x.BoneIndex).Distinct().OrderBy(x => x).ToList();

            var newSkel = View3D.Animation.AnimationEditor.ExtractPartOfSkeleton(MainAsset.Skeleton, "TestSkel", selectedBoneIds.ToArray());
            var newAnim = View3D.Animation.AnimationEditor.ExtractPartOfAnimation(MainAsset.AnimationClip, selectedBoneIds.ToArray());

            // Make root node world from skeleton
            var worldRotBone0 = newSkel.DynamicFrames[0].Quaternion[0];
            for (int i = 0; i < newAnim.DynamicFrames.Count; i++)
                newAnim.DynamicFrames[i].Rotation[0] = new Quaternion(worldRotBone0.X, worldRotBone0.Y, worldRotBone0.Z, worldRotBone0.W);

            Data.SetSkeleton(newSkel, newSkel.Header.SkeletonName);
            var skeletonPos = Data.Skeleton.GetWorldTransform(0).Translation;
            Data.Skeleton.SetBoneTransform(0, Vector3.Zero);

            Data.CopyMeshFromOther(MainAsset);
            var remapping = CreateBoneMapping(Data.Skeleton, MainAsset.Skeleton);
            Data.OnlyShowMeshRelatedToBones(selectedBoneIds, remapping, newSkel.Header.SkeletonName);
            Data.SetAnimationClip(newAnim, null);

            Data.SetMeshPosition(Matrix.CreateTranslation(-skeletonPos));
        }

        List<IndexRemapping> CreateBoneMapping(GameSkeleton newSkeleton, GameSkeleton originalSkeleton)
        {
            var output = new List<IndexRemapping>();
            for (int i = 0; i < newSkeleton.BoneCount; i++)
            {
                var oldIndex = originalSkeleton.GetBoneIndexByName(newSkeleton.BoneNames[i]);
                output.Add(new IndexRemapping(oldIndex, i));
            }

            return output;
        }

    }


    class SkeletonHelper
    {
        public static ObservableCollection<SkeletonBoneNode> CreateFlatSkeletonList(GameSkeleton skeleton, int startBone = -1)
        {
            var output = new ObservableCollection<SkeletonBoneNode>();
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                if (startBone == -1 || IsIndirectChildOf(i, startBone, skeleton))
                {
                    var bone = new SkeletonBoneNode()
                    {
                        BoneIndex = i,
                        BoneName = skeleton.BoneNames[i]
                    };
                    output.Add(bone);
                }
            }
            return output;
        }

        public static bool IsIndirectChildOf(int boneIndex, int childOff, GameSkeleton skeleton)
        {
            var parentIndex = skeleton.GetParentBone(boneIndex);
            if (parentIndex == -1)
                return false;

            if (parentIndex == childOff)
                return true;

            return IsIndirectChildOf(parentIndex, childOff, skeleton);
        }
    }

    // Flat skeleton
    //      Create from tree
    //      Create from sub
}
