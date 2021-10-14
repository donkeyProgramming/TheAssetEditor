using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.MathViews;
using CommonControls.Services;
using Filetypes.RigidModel;
using KitbasherEditor.Views.EditorViews.MeshFitter;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.MeshFitter
{
    public class MeshFitterViewModel : BoneMappingViewModel
    {
        Window _window;

        GameSkeleton _targetSkeleton; 
        GameSkeleton _fromSkeleton;

        IComponentManager _componentManager;
        AnimationClip _animationClip;
        AnimationPlayer _animationPlayer;
        AnimationPlayer _oldAnimationPlayer;
        List<Rmv2MeshNode> _meshNodes;
        SkeletonNode _currentSkeletonNode;

        public NotifyAttr<bool> RelativeScale { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel ScaleFactor { get; set; } = new DoubleViewModel(1);

        public Vector3ViewModel SkeletonDisplayOffset { get; set; } = new Vector3ViewModel(0);

        public NotifyAttr<bool> IsBoneSelected { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel BoneScaleFactor { get; set; } = new DoubleViewModel(1);
        public Vector3ViewModel BonePositionOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel BoneRotationOffset { get; set; } = new Vector3ViewModel(0);

        public MeshFitterViewModel(Window ownerWindow, RemappedAnimatedBoneConfiguration configuration, List<Rmv2MeshNode> meshNodes, GameSkeleton targetSkeleton, AnimationFile currentSkeletonFile, IComponentManager componentManager) : base(configuration)
        {
            _window = ownerWindow;

            _meshNodes = meshNodes;
            _targetSkeleton = targetSkeleton;
            _componentManager = componentManager;
            ScaleFactor.PropertyChanged += (_0, _1) => ApplyMeshFittingTransforms();
            BoneScaleFactor.PropertyChanged+=(_0, _1) => BoneScaleUpdate((float)BoneScaleFactor.Value, MeshBones.SelectedItem);
            BonePositionOffset.OnValueChanged += (viewModel) => BonePositionUpdated(viewModel, MeshBones.SelectedItem);
            BoneRotationOffset.OnValueChanged += (viewModel) => BoneRotationUpdated(viewModel, MeshBones.SelectedItem);
            SkeletonDisplayOffset.OnValueChanged += (viewModel) => SkeletonDisplayOffsetUpdated(viewModel);
            RelativeScale.PropertyChanged += (_0, _1) => ApplyMeshFittingTransforms();

            MeshBones.SelectedItemChanged += (_) => OnBoneSelected();

            _animationPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), "Temp animation rerig");
            _fromSkeleton = new GameSkeleton(currentSkeletonFile, _animationPlayer);
            
            // Build empty animation
            _animationClip = new AnimationClip();
            _animationClip.DynamicFrames.Add(new AnimationClip.KeyFrame());

            for (int i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[0].Rotation.Add(_fromSkeleton.Rotation[i]);
                _animationClip.DynamicFrames[0].Position.Add(_fromSkeleton.Translation[i]);
                _animationClip.DynamicFrames[0].Scale.Add(Vector3.One);

                _animationClip.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                _animationClip.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
            }

            _animationPlayer.SetAnimation(_animationClip, _fromSkeleton);
            _animationPlayer.Play();

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            _currentSkeletonNode = new SkeletonNode(resourceLib.Content, new SimpleSkeletonProvider(_fromSkeleton));
            _currentSkeletonNode.SelectedNodeColour = Color.White;
            _componentManager.GetComponent<SceneManager>().RootNode.AddObject(_currentSkeletonNode);

            _oldAnimationPlayer = _meshNodes.First().AnimationPlayer;
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _animationPlayer;
        }

        private void SkeletonDisplayOffsetUpdated(Vector3ViewModel viewModel)
        {
            _currentSkeletonNode.ModelMatrix = Matrix.CreateTranslation((float)viewModel.X.Value, (float)viewModel.Y.Value, (float)viewModel.Z.Value);
        }

        public override void AutoMapSelfAndChildrenByName()
        {
            base.AutoMapSelfAndChildrenByName();
            ApplyMeshFittingTransforms();
        }

        public override void AutoMapSelfAndChildrenByHierarchy()
        {
            base.AutoMapSelfAndChildrenByHierarchy();
            ApplyMeshFittingTransforms();
        }
        public override void ClearBindingSelfAndChildren()
        {
            base.ClearBindingSelfAndChildren();
            ApplyMeshFittingTransforms();
        }

        void OnBoneSelected()
        {
            IsBoneSelected.Value = MeshBones.SelectedItem != null;
            if (MeshBones.SelectedItem != null)
            {
                BoneScaleFactor.Value = MeshBones.SelectedItem.BoneScaleOffset;
                BoneRotationOffset.Set(MeshBones.SelectedItem.BoneRotOffset.X, MeshBones.SelectedItem.BoneRotOffset.Y, MeshBones.SelectedItem.BoneRotOffset.Z);
                BonePositionOffset.Set(MeshBones.SelectedItem.BonePosOffset.X, MeshBones.SelectedItem.BonePosOffset.Y, MeshBones.SelectedItem.BonePosOffset.Z);

                _currentSkeletonNode.SelectedBoneIndex = MeshBones.SelectedItem.BoneIndex.Value;
                _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Skeleton.SelectedBoneIndex = MeshBones.SelectedItem.MappedBoneIndex.Value;
            }
            else
            {
                BoneScaleFactor.Value = 1;
                BoneRotationOffset.Set(0);
                BonePositionOffset.Set(0);

                _currentSkeletonNode.SelectedBoneIndex = null;
                _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Skeleton.SelectedBoneIndex = null;
            }
        }

        void BoneScaleUpdate(float newValue, AnimatedBone bone)
        {
            if (bone == null)
                return;
            bone.BoneScaleOffset = newValue;
            ApplyMeshFittingTransforms();
        }

        void BoneRotationUpdated(Vector3ViewModel newValue, AnimatedBone bone)
        {
            if (bone == null)
                return;
            bone.BoneRotOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ApplyMeshFittingTransforms();
        }

        void BonePositionUpdated(Vector3ViewModel newValue, AnimatedBone bone)
        {
            if (bone == null)
                return;
            bone.BonePosOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ApplyMeshFittingTransforms();
        }

        void ApplyMeshFittingTransforms()
        {
            // Rebuild the mapping index as its easy to work with
            var mapping = AnimatedBoneHelper.BuildRemappingList(MeshBones.PossibleValues.First());

            // Reset the animation back to bind pose
            for (int i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[0].Rotation[i] = _fromSkeleton.Rotation[i];
                _animationClip.DynamicFrames[0].Position[i] = _fromSkeleton.Translation[i];
                _animationClip.DynamicFrames[0].Scale[i] = Vector3.One;
            }

            // Set the base scale for the mesh and apply the animation
            float baseScale = (float)ScaleFactor.Value;
            _animationClip.DynamicFrames[0].Scale[0] = new Vector3(baseScale);
            _animationPlayer.Refresh();

            if (baseScale == 0)
                return;

            for (int i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                var mappedIndex = mapping.FirstOrDefault(x => x.OriginalValue == i);
                var boneValuesObject = MeshBones.PossibleValues.First().GetFromBoneId(i);

                var fromBoneIndex = i;
                var fromParentBoneIndex = _fromSkeleton.GetParentBone(fromBoneIndex);
                Matrix desiredBonePosWorld = Matrix.Identity;

                // Get the world position where we want to move the bone to
                if (mappedIndex != null)
                {
                    var targetBoneIndex = mappedIndex.NewValue;
                    desiredBonePosWorld = _targetSkeleton.GetAnimatedWorldTranform(targetBoneIndex);
                }
                else
                {
                    desiredBonePosWorld = _fromSkeleton.GetAnimatedWorldTranform(i);
                }

                // Apply the offset values to the bone in worldspace
                var desiredBonePosWorldWithOffsets = MathUtil.CreateRotation(boneValuesObject.BoneRotOffset) *
                    desiredBonePosWorld *
                    Matrix.CreateTranslation(boneValuesObject.BonePosOffset);

                // Apply relative scale if applicable 
                float relativeScale = 1;
                var computeRelativeScale = RelativeScale.Value && mappedIndex != null;
                if (computeRelativeScale)
                {
                    var targetBoneIndex = mappedIndex.NewValue;
                    var targetParentBoneIndex = _targetSkeleton.GetParentBone(targetBoneIndex);

                    if (fromParentBoneIndex != -1 && targetParentBoneIndex != -1)
                    {
                        var toBone0 = _targetSkeleton.GetWorldTransform(targetBoneIndex);
                        var toBone1 = _targetSkeleton.GetWorldTransform(targetParentBoneIndex);
                        var targetBoneLength = Vector3.Distance(toBone0.Translation, toBone1.Translation);

                        var fromBone0 = _fromSkeleton.GetWorldTransform(fromBoneIndex);
                        var fromBone1 = _fromSkeleton.GetWorldTransform(fromParentBoneIndex);
                        var fromBoneLength = Vector3.Distance(fromBone0.Translation, fromBone1.Translation);

                        relativeScale = fromBoneLength / targetBoneLength;
                    }
                }

                // Compute scaling
                float scale = boneValuesObject.BoneScaleOffset * relativeScale;

                // To stop the calculations from exploding with NAN values
                if (scale <= 0 || float.IsNaN(scale))
                    scale = 0.00001f;   

                var parentWorld = Matrix.Identity;
                if (fromParentBoneIndex != -1)
                    parentWorld = _fromSkeleton.GetAnimatedWorldTranform(fromParentBoneIndex);
                var bonePositionLocalSpace = desiredBonePosWorldWithOffsets * Matrix.Invert(parentWorld);
                bonePositionLocalSpace.Decompose(out var _,  out var boneRotation,  out var bonePosition);

                // Apply the values to the animation
                _animationClip.DynamicFrames[0].Rotation[i] = boneRotation;
                _animationClip.DynamicFrames[0].Position[i] = bonePosition;
                _animationClip.DynamicFrames[0].Scale[i] *= new Vector3(scale);

                // Apply the inv scale to all children to avoid the mesh growing out of control
                var childBones = _fromSkeleton.GetDirectChildBones(i);
                foreach (var childBoneIndex in childBones)
                {
                    float invScale = 1 / scale;
                    _animationClip.DynamicFrames[0].Scale[childBoneIndex] *= new Vector3(invScale);
                }

                _animationPlayer.Refresh();
            }
        }

        public override void OnMappingCreated(int humanoid01BoneIndex, int dwarfBoneIndex)
        {
            if (_targetSkeleton == null)
                return;

            ApplyMeshFittingTransforms();
        }

        public void ResetOffsetTransforms()
        {
            if (MeshBones.SelectedItem != null)
            {
                MeshBones.SelectedItem.BonePosOffset = Vector3.Zero;
                MeshBones.SelectedItem.BoneRotOffset = Vector3.Zero;
                MeshBones.SelectedItem.BoneScaleOffset = 1;
                OnBoneSelected();
            }
        
        }

        public void CopyScaleToChildren()
        {
            if (MeshBones.SelectedItem != null)
            {
                var id = MeshBones.SelectedItem.BoneIndex.Value;
                var childBones = _fromSkeleton.GetAllChildBones(id);
                foreach (var boneId in childBones)
                {
                    var bone = MeshBones.PossibleValues.First().GetFromBoneId(boneId);
                    bone.BoneScaleOffset = MeshBones.SelectedItem.BoneScaleOffset;
                }

                ApplyMeshFittingTransforms();
            }
        }


        public void CleanUp()
        {
            // Restore animation player
            _componentManager.GetComponent<AnimationsContainerComponent>().Remove(_animationPlayer);
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _oldAnimationPlayer;

            // Remove the skeleton node
            _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Skeleton.SelectedBoneIndex = null; 
            _componentManager.GetComponent<SceneManager>().RootNode.RemoveObject(_currentSkeletonNode);
        }

        public void SaveAndClose()
        {
            var frame = AnimationSampler.Sample(0, _fromSkeleton, _animationClip);
            var cmd = new CreateAnimatedMeshPoseCommand(_meshNodes, frame);
            _componentManager.GetComponent<CommandExecutor>().ExecuteCommand(cmd);   
            _window.Close();
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
            {
                var commaList = string.Join(",", allSkeltonNames);
                MessageBox.Show($"Unexpected number of skeletons - {commaList}. This tool only works for one skeleton");
                return;
            }

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
            config.ParentModelBones= AnimatedBoneHelper.CreateFromSkeleton(targetSkeletonFile);

            config.MeshSkeletonName = currentSkeletonName;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(currentSkeletonFile, usedBoneIndexes);


            var containingWindow = new Window();
            containingWindow.Title = "MeshFitter";
            containingWindow.Width = 1200;
            containingWindow.Height = 1100;
            containingWindow.DataContext = new MeshFitterViewModel(containingWindow, config, meshNodes, targetSkeleton.AnimationProvider.Skeleton, currentSkeletonFile, componentManager);
            containingWindow.Content = new MeshFitterView();
            containingWindow.Closed += ContainingWindow_Closed;
            containingWindow.Show();
        }

        private static void ContainingWindow_Closed(object sender, EventArgs e)
        {
            var window = sender as Window;
            var dataContex = window.DataContext as MeshFitterViewModel;
            dataContex.CleanUp();
        }
    }
}
