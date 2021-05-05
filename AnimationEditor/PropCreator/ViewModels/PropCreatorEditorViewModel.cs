using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class PropCreatorEditorViewModel : NotifyPropertyChangedImpl
    {
        public AssetViewModel MainAsset { get; set; }
        public AssetViewModel ReferenceAsset { get; set; }


        //
        // long? _selectedValue;
        // public long? SelectedValue
        // {
        //     get { return _selectedValue; }
        //     set { SetField(ref _selectedValue, value); }
        // }
        //

        IReadOnlyList<SkeletonBoneNode> _startBoneList;
         public IReadOnlyList<SkeletonBoneNode> StartBoneList
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



        IReadOnlyList<SkeletonBoneNode> _endBoneList;
        public IReadOnlyList<SkeletonBoneNode> EndBoneList
        {
            get { return _endBoneList; }
            set { SetAndNotify(ref _endBoneList, value); }
        }

        SkeletonBoneNode _selectedEndBone;
        public SkeletonBoneNode SelectedEndBone
        {
            get { return _selectedEndBone; }
            set { SetAndNotify(ref _selectedEndBone, value); EndBoneSelected(); }
        }


        IReadOnlyList<SkeletonBoneNode> _referenceBoneList;
        public IReadOnlyList<SkeletonBoneNode> ReferenceBoneList
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
            set { SetAndNotify(ref _snapToRefBone, value);}
        }

        AssetViewModel _data;
        public AssetViewModel Data { get => _data; set => SetAndNotify(ref _data, value); }


        public PropCreatorEditorViewModel(PackFileService pfs, AssetViewModel mainAsset, AssetViewModel refAsset)
        {
            Data = new AssetViewModel(pfs);
            MainAsset = mainAsset;
            ReferenceAsset = refAsset;

            MainAsset.SkeletonChanged += MainAsset_SkeletonChanged;
            refAsset.SkeletonChanged += RefAsset_SkeletonChanged;
        }

        private void RefAsset_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ReferenceBoneList = null;
                return;
            }

            ReferenceBoneList = SkeletonHelper.CreateFlatSkeletonList(newValue);
        }

        private void MainAsset_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
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


        private void EndBoneSelected()
        {
            if (SelectedStartBone == null || SelectedEndBone == null || SelectedStartBone == null)
                return;

            var boneList = SkeletonHelper.CreateFlatSkeletonList(MainAsset.Skeleton, SelectedStartBone.BoneIndex);
            boneList.Insert(0, SelectedStartBone);
            var selectedBones = boneList.Where(x => !SkeletonHelper.IsIndirectChildOf(x.BoneIndex, SelectedEndBone.BoneIndex, MainAsset.Skeleton)).ToList();
            var selectedBoneIds = boneList.Select(x => x.BoneIndex).Distinct().OrderBy(x=>x).ToList();

            MainAsset.OnlyShowMeshRelatedToBones(selectedBoneIds);

            try
            {
                var newSkel = View3D.Animation.AnimationEditor.ExtractPartOfSkeleton(MainAsset.Skeleton, "TestSkel", selectedBoneIds.ToArray());
                var newAnim = View3D.Animation.AnimationEditor.ExtractPartOfAnimation(MainAsset.AnimationClip, selectedBoneIds.ToArray());

               

                var worldRotBone0 = newSkel.DynamicFrames[0].Quaternion[0];
                for (int i = 0; i < newAnim.DynamicFrames.Count; i++)
                    newAnim.DynamicFrames[i].Rotation[0] = new Quaternion(
                    worldRotBone0.X,
                    worldRotBone0.Y,
                    worldRotBone0.Z,
                    worldRotBone0.W);






                Data.SetSkeleton(newSkel, newSkel.Header.SkeletonName);
                Data.SetAnimationClip(newAnim);

                Data.ResetAnimation();
                MainAsset.ResetAnimation();
                ReferenceAsset.ResetAnimation();
            }
            catch (Exception e)
            { 
            }

            
        }

        void CreateAnimatedProp(AssetViewModel asset, List<SkeletonBoneNode> bones)
        { 
            // Create skeleton
            // Create mesh
                // Only show parts
                // Remap bone ids
            // Create animation
        }

    }


    class SkeletonHelper
    {
        public static List<SkeletonBoneNode> CreateFlatSkeletonList(View3D.Animation.GameSkeleton skeleton, int startBone = -1)
        {
            List<SkeletonBoneNode> output = new List<SkeletonBoneNode>();
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

        public static bool IsIndirectChildOf(int boneIndex, int childOff, View3D.Animation.GameSkeleton skeleton)
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
