using CommonControls.Common;
using CommonControls.MathViews;
using CommonControls.Services;
using Filetypes.RigidModel;
using KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping;
using KitbasherEditor.Views.EditorViews.MeshFitter;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.MeshFitter
{
    public class MeshFitterViewModel : AnimatedBlendIndexRemappingViewModel
    {
        GameSkeleton _dwarfSkeleton; 
        GameSkeleton _humanoid01Skeleton;

        IComponentManager _componentManager;
        AnimationClip _animationClip;
        AnimationPlayer _animationPlayer;
        AnimationPlayer _oldAnimationPlayer;
        List<Rmv2MeshNode> _meshNodes;
        SkeletonNode _currentSkeletonNode;

        public NotifyAttr<bool> RelativeScale { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel ScaleFactor { get; set; } = new DoubleViewModel(1);

        public NotifyAttr<bool> IsBoneSelected { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel BoneScaleFactor { get; set; } = new DoubleViewModel(1);
        public Vector3ViewModel BonePositionOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel BoneRotationOffset { get; set; } = new Vector3ViewModel(0);

        public MeshFitterViewModel(RemappedAnimatedBoneConfiguration configuration, List<Rmv2MeshNode> meshNodes, GameSkeleton targetSkeleton, AnimationFile currentSkeletonFile, IComponentManager componentManager) : base(configuration)
        {
            _meshNodes = meshNodes;
            _dwarfSkeleton = targetSkeleton;
            _componentManager = componentManager;
            ScaleFactor.PropertyChanged += (_0, _1) => ReProcessFucker();
            BoneScaleFactor.PropertyChanged+=(_0, _1) => BoneScaleUpdate((float)BoneScaleFactor.Value, MeshBones.SelectedBone);
            BonePositionOffset.OnValueChanged += (viewModel) => BonePositionUpdated(viewModel, MeshBones.SelectedBone);
            BoneRotationOffset.OnValueChanged += (viewModel) => BoneRotationUpdated(viewModel, MeshBones.SelectedBone);
            RelativeScale.PropertyChanged += (_0, _1) => ReProcessFucker();

            MeshBones.BoneSelected += (_) => OnBoneSelected();

            _animationPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), "Temp animation rerig");
            _humanoid01Skeleton = new GameSkeleton(currentSkeletonFile, _animationPlayer);
            
            // Build empty animation
            _animationClip = new AnimationClip();
            _animationClip.DynamicFrames.Add(new AnimationClip.KeyFrame());

            for (int i = 0; i < _humanoid01Skeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[0].Rotation.Add(_humanoid01Skeleton.Rotation[i]);
                _animationClip.DynamicFrames[0].Position.Add(_humanoid01Skeleton.Translation[i]);
                _animationClip.DynamicFrames[0].Scale.Add(Vector3.One);

                _animationClip.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                _animationClip.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
            }

            _animationPlayer.SetAnimation(_animationClip, _humanoid01Skeleton);
            _animationPlayer.Play();

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            _currentSkeletonNode = new SkeletonNode(resourceLib.Content, new SimpleSkeletonProvider(_humanoid01Skeleton));
            _componentManager.GetComponent<SceneManager>().RootNode.AddObject(_currentSkeletonNode);

            _oldAnimationPlayer = _meshNodes.First().AnimationPlayer;
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _animationPlayer;
        }

        public override void AutoMapSelfAndChildrenByName()
        {
            base.AutoMapSelfAndChildrenByName();
            ReProcessFucker();
        }

        public override void AutoMapSelfAndChildrenByHierarchy()
        {
            base.AutoMapSelfAndChildrenByHierarchy();
            ReProcessFucker();
        }
        public override void ClearBindingSelfAndChildren()
        {
            base.ClearBindingSelfAndChildren();
            ReProcessFucker();
        }

        void OnBoneSelected()
        {
            IsBoneSelected.Value = MeshBones.SelectedBone != null;
            if (MeshBones.SelectedBone != null)
            {
                BoneScaleFactor.Value = MeshBones.SelectedBone.BoneScaleOffset;
                BoneRotationOffset.Set(MeshBones.SelectedBone.BoneRotOffset.X, MeshBones.SelectedBone.BoneRotOffset.Y, MeshBones.SelectedBone.BoneRotOffset.Z);
                BonePositionOffset.Set(MeshBones.SelectedBone.BonePosOffset.X, MeshBones.SelectedBone.BonePosOffset.Y, MeshBones.SelectedBone.BonePosOffset.Z);
            }
            else
            {
                BoneScaleFactor.Value = 1;
                BoneRotationOffset.Set(0);
                BonePositionOffset.Set(0);
            }
        }

        void BoneScaleUpdate(float newValue, AnimatedBone bone)
        {
            bone.BoneScaleOffset = newValue;
            ReProcessFucker();
        }

        void BoneRotationUpdated(Vector3ViewModel newValue, AnimatedBone bone)
        {
            bone.BoneRotOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ReProcessFucker();
        }

        void BonePositionUpdated(Vector3ViewModel newValue, AnimatedBone bone)
        {
            bone.BonePosOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ReProcessFucker();
        }

        void ReProcessFucker()
        {
            // From humanoid to dwarf

            var mapping = MeshBones.Bones.First().BuildRemappingList();

            // Reset it all
            for (int i = 0; i < _humanoid01Skeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[0].Rotation[i] = _humanoid01Skeleton.Rotation[i];
                _animationClip.DynamicFrames[0].Position[i] = _humanoid01Skeleton.Translation[i];
                _animationClip.DynamicFrames[0].Scale[i] = Vector3.One;
            }

            // Set scale
            float baseScale = (float)ScaleFactor.Value;
            
            _animationClip.DynamicFrames[0].Scale[0] = new Vector3(baseScale);
            _animationPlayer.Refresh();
            if (baseScale == 0)
                return;
            for (int i = 0; i < _humanoid01Skeleton.BoneCount; i++)
            {
                var mappedIndex = mapping.FirstOrDefault(x => x.OriginalValue == i);
                var boneAttribute = MeshBones.GetFromBoneId(i);
                if (mappedIndex != null)
                {
                    var dwarfBoneIndex = mappedIndex.NewValue; ;
                    var humanoid01BoneIndex = mappedIndex.OriginalValue;

                    var desiredParentWorldTransform = _dwarfSkeleton.GetWorldTransform(dwarfBoneIndex);

                    desiredParentWorldTransform =
                            (Matrix.CreateRotationX(boneAttribute.BoneRotOffset.X) * Matrix.CreateRotationY(boneAttribute.BoneRotOffset.Y) * Matrix.CreateRotationZ(boneAttribute.BoneRotOffset.Z)) *
                        desiredParentWorldTransform * 
                    
                        Matrix.CreateTranslation(boneAttribute.BonePosOffset);


                    var dwarfParentBone = _dwarfSkeleton.GetParentBone(dwarfBoneIndex) ;
                    if (dwarfParentBone == -1)
                        continue;


                    var dwarfParentWorldPos = _dwarfSkeleton.GetWorldTransform(dwarfParentBone);
                    var dwarfBoneLength = Vector3.Distance(desiredParentWorldTransform.Translation, dwarfParentWorldPos.Translation);


                    float scale = 1;
                    scale = scale * boneAttribute.BoneScaleOffset;
                    var parentBoneIndex = _humanoid01Skeleton.GetParentBone(humanoid01BoneIndex);
                    if (parentBoneIndex == -1)
                        continue;

                    var parentWorld = _humanoid01Skeleton.GetAnimatedWorldTranform(parentBoneIndex);
                    var matrix = desiredParentWorldTransform * Matrix.Invert(parentWorld);
                    matrix.Decompose(out var _, out var newRotation, out var newPosition);


                    var humanodB0 = _humanoid01Skeleton.GetWorldTransform(humanoid01BoneIndex);
                    if (humanoid01BoneIndex != -1 && parentBoneIndex != -1)
                    {
                        var humanodB1 = _humanoid01Skeleton.GetWorldTransform(parentBoneIndex);
                        var humanodBoneLength = Vector3.Distance(humanodB0.Translation, humanodB1.Translation);

                        if(RelativeScale.Value)
                            scale = (dwarfBoneLength / humanodBoneLength) *(baseScale * boneAttribute.BoneScaleOffset);

                        if (float.IsNaN(scale))
                            scale = baseScale;

                    }
                   
                    _animationClip.DynamicFrames[0].Rotation[humanoid01BoneIndex] = newRotation;
                    _animationClip.DynamicFrames[0].Position[humanoid01BoneIndex] = newPosition;
                    _animationClip.DynamicFrames[0].Scale[humanoid01BoneIndex] *= new Vector3(scale);

                    var childBones = _humanoid01Skeleton.GetChildBones(humanoid01BoneIndex);
                    foreach (var childBoneIndex in childBones)
                    {
                        float invScale = 1 / scale;
                        _animationClip.DynamicFrames[0].Scale[childBoneIndex] *= new Vector3(invScale);
                    }

                    _animationPlayer.Refresh();
                }
            }
        }

        public override void OnMappingCreated(int humanoid01BoneIndex, int dwarfBoneIndex)
        {
            if (_dwarfSkeleton == null)
                return;

            ReProcessFucker();
        }


        public void Close()
        {
            // Restore animation player
            _componentManager.GetComponent<AnimationsContainerComponent>().Remove(_animationPlayer);
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _oldAnimationPlayer;

            // Apply changes to mesh

            // Remove the skeleton node
            _componentManager.GetComponent<SceneManager>().RootNode.RemoveObject(_currentSkeletonNode);
        }

        public static void ShowView(List<ISelectable> meshesToFit, IComponentManager componentManager, SkeletonAnimationLookUpHelper skeletonHelper, PackFileService pfs)
        {
            var sceneManager = componentManager.GetComponent<SceneManager>();
            var resourceLib = componentManager.GetComponent<ResourceLibary>();
            var animCollection = componentManager.GetComponent<AnimationsContainerComponent>();

            var meshNodes = meshesToFit
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            var allSkeltonNames = meshNodes
                .Select(x => x.MeshModel.ParentSkeletonName)
                .Distinct();

            if (allSkeltonNames.Count() != 1)
                throw new Exception("Unexpected number of skeletons. This tool only works for one skeleton");

            var currentSkeletonName = allSkeltonNames.First();
            var currentSkeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, currentSkeletonName);

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x=>(int)x)
                .ToList();

            var targetSkeleton = componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Skeleton;
            var targetSkeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, targetSkeleton.Name);
            
            RemappedAnimatedBoneConfiguration config = new RemappedAnimatedBoneConfiguration();
            config.ParnetModelSkeletonName= targetSkeleton.Name;
            config.ParentModelBones= AnimatedBone.CreateFromSkeleton(targetSkeletonFile);

            config.MeshSkeletonName = currentSkeletonName;
            config.MeshBones = AnimatedBone.CreateFromSkeleton(currentSkeletonFile, usedBoneIndexes);


            var containingWindow = new Window();
            containingWindow.Title = "Texture Preview Window";
            containingWindow.DataContext = new MeshFitterViewModel(config, meshNodes, targetSkeleton.AnimationProvider.Skeleton, currentSkeletonFile, componentManager);
            containingWindow.Content = new MeshFitterView();
            containingWindow.Closed += ContainingWindow_Closed;
            containingWindow.Show();
        }

        private static void ContainingWindow_Closed(object sender, EventArgs e)
        {
            var window = sender as Window;
            var dataContex = window.DataContext as MeshFitterViewModel;
            dataContex.Close();
        }
    }
}
