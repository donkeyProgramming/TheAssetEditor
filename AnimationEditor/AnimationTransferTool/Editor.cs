using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.MathViews;
using CommonControls.Services;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.SceneNodes;
using View3D.Utility;

namespace AnimationEditor.AnimationTransferTool
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AssetViewModel _copyTo;
        AssetViewModel _copyFrom;
        AssetViewModel _generated;
        List<IndexRemapping> _remappingInformaton;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public NotifyAttr<string> MappingStatusText { get; set; } = new NotifyAttr<string>("Status: Error");
        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value);  }
        }

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel copyToAsset, AssetViewModel copyFromAsset, AssetViewModel generated, IComponentManager componentManager)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            _generated = generated;

      

            _copyTo.SkeletonChanged += Target_SkeletonChanged;
            _copyFrom.SkeletonChanged += _source_SkeletonChanged;
            _copyTo.MeshChanged += MeshChanged;

            _copyTo.Offset = Matrix.CreateTranslation(new Vector3(0, 0, -1));
            _copyFrom.Offset = Matrix.CreateTranslation(new Vector3(0, 0, 1));
            // _source.MainNode.

            

            MeshChanged(_copyTo);
        }

        void OnBoneSelected(SkeletonBoneNode b)
        { }

        private void MeshChanged(AssetViewModel newValue)
        {
            _generated.CopyMeshFromOther(newValue, true);
            CreateBoneOverview(newValue.Skeleton);
        }

        private void _source_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            //throw new NotImplementedException();
        }

        private void Target_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            //throw new NotImplementedException();
        }

        public void OpenMappingWindow()
        {
            var targetSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyTo.SkeletonName);
            var sourceSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.SkeletonName);

            var config = new RemappedAnimatedBoneConfiguration();
            config.MeshSkeletonName = _copyTo.SkeletonName;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeleton);

            config.ParnetModelSkeletonName = _copyFrom.SkeletonName;
            config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(sourceSkeleton);

            if (config.MeshSkeletonName == config.ParnetModelSkeletonName)
                MessageBox.Show("The two models share skeleton, no need for mapping?", "Error", MessageBoxButton.OK);

            var window = new BoneMappingWindow(new BoneMappingViewModel(config));
            if (window.ShowDialog() == true)
            {
                _remappingInformaton = AnimatedBoneHelper.BuildRemappingList(config.MeshBones.First());
                Process();
                UpdateBonesAfterMapping(Bones);
                //_componentManager.GetComponent<CommandExecutor>().ExecuteCommand(new RemapBoneIndexesCommand(selectedMeshses, remapping, config.ParnetModelSkeletonName));
            }
        }

        void UpdateBonesAfterMapping(IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                bone.HasMapping.Value = mapping != null;
                UpdateBonesAfterMapping(bone.Children);
            }
        }

        
        public void Process()
        {
            if (_remappingInformaton == null)
            {
                MessageBox.Show("No config?", "Error", MessageBoxButton.OK);
                return;
            }

            AnimationClip clip = ApplyMeshFittingTransforms(_copyFrom.Skeleton, _copyTo.Skeleton, _copyFrom.AnimationClip);
            _generated.SetAnimationClip(clip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
        }


        AnimationClip CreateNewAnimation(GameSkeleton skeleton, AnimationClip animationToCopy)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;

            var newAnimation = new AnimationClip();
            newAnimation.PlayTimeInSec = animationToCopy.PlayTimeInSec;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                newAnimation.DynamicFrames.Add(new AnimationClip.KeyFrame());
                for (int i = 0; i < skeleton.BoneCount; i++)
                {
                    newAnimation.DynamicFrames[frameIndex].Rotation.Add(skeleton.Rotation[i]);
                    newAnimation.DynamicFrames[frameIndex].Position.Add(skeleton.Translation[i]);
                    newAnimation.DynamicFrames[frameIndex].Scale.Add(Vector3.One);
                }
            }

            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                newAnimation.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                newAnimation.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                newAnimation.DynamicFrames[0].Scale[0] = Vector3.One;
            }
            return newAnimation;
        }

        AnimationClip ApplyMeshFittingTransforms(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToCopy)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;
            var newAnimation = CreateNewAnimation(copyToSkeleton, animationToCopy);

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (int i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentCopyToFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, new List<AnimationClip>() { newAnimation });
                    var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, new List<AnimationClip>() { animationToCopy });

                    var boneSettings = GetBoneFromId(Bones, i);
                    var mappedIndex = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == i);

                    var fromParentBoneIndex = copyToSkeleton.GetParentBone(i);
                    var desiredBonePosWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, i);

                    if (mappedIndex != null)
                    {
                        var targetBoneIndex = mappedIndex.NewValue;
                        desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);
                    }

                    // Apply single bone offset
                    desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.SingleBoneRotationOffset.X.Value, (float)boneSettings.SingleBoneRotationOffset.Y.Value, (float)boneSettings.SingleBoneRotationOffset.Z.Value)) *
                        desiredBonePosWorld *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.SingleBoneTranslationOffset.X.Value, (float)boneSettings.SingleBoneTranslationOffset.Y.Value, (float)boneSettings.SingleBoneTranslationOffset.Z.Value));

                    var parentWorld = Matrix.Identity;
                    if (fromParentBoneIndex != -1)
                    {
                        parentWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);

                        var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                        bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                        newAnimation.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                        newAnimation.DynamicFrames[frameIndex].Position[i] = bonePosition;
                    }
                }
            }

            //ApplyRelativeScale(copyFromSkeleton, copyToSkeleton, newAnimation, animationToCopy);
            //SnapBonesToWorld(copyFromSkeleton, copyToSkeleton, newAnimation, animationToCopy);
            ApplyHirachyMovement(copyFromSkeleton, copyToSkeleton, newAnimation, animationToCopy);
            return newAnimation;
        }

        void ApplyRelativeScale(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale, AnimationClip animationToCopy)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (int i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var boneSettings = GetBoneFromId(Bones, i);
                    var mappedIndex = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == i);

                    if (mappedIndex != null)
                    {
                        var targetBoneIndex = mappedIndex.NewValue;
                        var copyFromParentIndex = copyFromSkeleton.GetParentBone(targetBoneIndex);
                        var copyToParentIndex = copyToSkeleton.GetParentBone(i);

                        if (copyToParentIndex != -1 && copyFromParentIndex != -1)
                        {
                            var toBone0 = copyToSkeleton.GetWorldTransform(i).Translation;
                            var toBone1 = copyToSkeleton.GetWorldTransform(copyToParentIndex).Translation;
                            var targetBoneLength = Vector3.Distance(toBone0, toBone1);

                            var fromBone0 = copyFromSkeleton.GetWorldTransform(targetBoneIndex).Translation;
                            var fromBone1 = copyFromSkeleton.GetWorldTransform(copyFromParentIndex).Translation;
                            var fromBoneLength = Vector3.Distance(fromBone0, fromBone1);

                            if (fromBoneLength == 0 || targetBoneLength == 0)
                            {
                                targetBoneLength = 1;
                                fromBoneLength = 1;
                            }

                            var relativeScale =  targetBoneLength / fromBoneLength;
                            animationToScale.DynamicFrames[frameIndex].Position[i] = animationToScale.DynamicFrames[frameIndex].Position[i] * relativeScale;
                        }
                    }
                }
            }
        }

        void SnapBonesToWorld(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale, AnimationClip animationToCopy)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, new List<AnimationClip>() { animationToCopy });

                for (int i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, new List<AnimationClip>() { animationToScale });
                    
                    var boneSettings = GetBoneFromId(Bones, i);
                    if (boneSettings.ForceSnapToWorld.Value == false)
                        continue;

                    var mappedIndex = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == i);
                    if (mappedIndex == null)
                        continue;

                    var fromParentBoneIndex = copyToSkeleton.GetParentBone(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var targetBoneIndex = mappedIndex.NewValue;
                    var desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);
                  
                    desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.SingleBoneRotationOffset.X.Value, (float)boneSettings.SingleBoneRotationOffset.Y.Value, (float)boneSettings.SingleBoneRotationOffset.Z.Value)) *
                        desiredBonePosWorld *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.SingleBoneTranslationOffset.X.Value, (float)boneSettings.SingleBoneTranslationOffset.Y.Value, (float)boneSettings.SingleBoneTranslationOffset.Z.Value));

                    var parentWorld = currentFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);

                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);
                   
                    // Apply the values to the animation
                    animationToScale.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    animationToScale.DynamicFrames[frameIndex].Position[i] = bonePosition;
                }
            }
        }

        void ApplyHirachyMovement(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale, AnimationClip animationToCopy)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, new List<AnimationClip>() { animationToCopy });

                for (int i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, new List<AnimationClip>() { animationToScale });

                    var boneSettings = GetBoneFromId(Bones, i);
                    if (!(boneSettings.TranslationOffset.X.Value != 0 || boneSettings.TranslationOffset.Y.Value != 0 || boneSettings.TranslationOffset.Z.Value != 0))
                        continue;

                    var mappedIndex = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == i);
                    if (mappedIndex == null)
                        continue;

                    var fromParentBoneIndex = copyToSkeleton.GetParentBone(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var targetBoneIndex = mappedIndex.NewValue;
                    var desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);

                    desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value)) *
                        desiredBonePosWorld *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.TranslationOffset.X.Value, (float)boneSettings.TranslationOffset.Y.Value, (float)boneSettings.TranslationOffset.Z.Value));

                    var parentWorld = currentFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);

                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    // Apply the values to the animation
                    animationToScale.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    animationToScale.DynamicFrames[frameIndex].Position[i] = bonePosition;
                }
            }
        }


        //---void 
        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
          
            if (skeleton == null)
                return;
            for (int i = 0; i<skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBone(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(new SkeletonBoneNode(skeleton.BoneNames[i], i, -1));
                }
                else
                {
                    var treeParent = GetBoneFromId(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(new SkeletonBoneNode(skeleton.BoneNames[i], i, parentBoneId));
                }
            }
        }

        //SkeletonBoneNode GetParent(ObservableCollection<SkeletonBoneNode> root, int parentBoneId)
        //{
        //    foreach (SkeletonBoneNode item in root)
        //    {
        //        if (item.BoneIndex.Value == parentBoneId)
        //            return item;
        //
        //        var result = GetParent(item.Children, parentBoneId);
        //        if (result != null)
        //            return result;
        //    }
        //    return null;
        //}

        SkeletonBoneNode GetBoneFromId(ObservableCollection<SkeletonBoneNode> root, int boneId)
        {
            foreach (SkeletonBoneNode item in root)
            {
                if (item.BoneIndex.Value == boneId)
                    return item;

                var result = GetBoneFromId(item.Children, boneId);
                if (result != null)
                    return result;
            }
            return null;
        }
    }

    public class SkeletonBoneNode : NotifyPropertyChangedImpl
    {
        public NotifyAttr<int> BoneIndex { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> ParnetBoneIndex { get; set; } = new NotifyAttr<int>(-1);
        public NotifyAttr<string> BoneName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> HasMapping { get; set; } = new NotifyAttr<bool>(false);

        public SkeletonBoneNode(string boneName, int boneIndex, int parentBoneIndex)
        {
            BoneName.Value = boneName;
            BoneIndex.Value = boneIndex;
            ParnetBoneIndex.Value = parentBoneIndex;
        }

        public Vector3ViewModel SingleBoneRotationOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel SingleBoneTranslationOffset { get; set; } = new Vector3ViewModel(0);

        public Vector3ViewModel RotationOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel TranslationOffset { get; set; } = new Vector3ViewModel(0);

        public NotifyAttr<bool> ForceSnapToWorld { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<SkeletonBoneNode> BoneIsRelativeTo { get; set; } = new NotifyAttr<SkeletonBoneNode>(null);    // Used for attachment points

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
    }
}
