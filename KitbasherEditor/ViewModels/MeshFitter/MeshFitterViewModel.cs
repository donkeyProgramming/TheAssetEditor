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

        public NotifyAttr<bool> IsBoneSelected { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel BoneScaleFactor { get; set; } = new DoubleViewModel(1);
        public Vector3ViewModel BonePositionOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel BoneRotationOffset { get; set; } = new Vector3ViewModel(0);

        public MeshFitterViewModel(RemappedAnimatedBoneConfiguration configuration, List<Rmv2MeshNode> meshNodes, GameSkeleton targetSkeleton, AnimationFile currentSkeletonFile, IComponentManager componentManager) : base(configuration)
        {
            _meshNodes = meshNodes;
            _targetSkeleton = targetSkeleton;
            _componentManager = componentManager;
            ScaleFactor.PropertyChanged += (_0, _1) => ReProcessFucker();
            BoneScaleFactor.PropertyChanged+=(_0, _1) => BoneScaleUpdate((float)BoneScaleFactor.Value, MeshBones.SelectedBone);
            BonePositionOffset.OnValueChanged += (viewModel) => BonePositionUpdated(viewModel, MeshBones.SelectedBone);
            BoneRotationOffset.OnValueChanged += (viewModel) => BoneRotationUpdated(viewModel, MeshBones.SelectedBone);
            RelativeScale.PropertyChanged += (_0, _1) => ReProcessFucker();

            MeshBones.BoneSelected += (_) => OnBoneSelected();

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
            // Rebuild the mapping index as its easy to work with
            var mapping = MeshBones.Bones.First().BuildRemappingList();

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
                var boneObject = MeshBones.GetFromBoneId(i);

                float boneScale = 1;
                var bonePosition = Vector3.Zero;// _fromSkeleton.get
                var boneRotation = Quaternion.Identity;// _animationClip.DynamicFrames[0].Rotation[i];

                var fromBoneIndex = i;

                if (mappedIndex != null)
                {
                    var targetBoneIndex = mappedIndex.NewValue;
                    

                    // Get the world position where we want to move the bone to
                    var targetBoneWorldTransform = _targetSkeleton.GetAnimatedWorldTranform(targetBoneIndex);

                    // Apply the offset values
                    var desiredParentWorldTransform = MathUtil.CreateRotation(boneObject.BoneRotOffset) * 
                        targetBoneWorldTransform *
                        Matrix.CreateTranslation(boneObject.BonePosOffset);

                    // Compute scaling
                    float scale = boneObject.BoneScaleOffset;
                    if (scale <= 0)
                        scale = 0.00001f;   // To stop the calculations from exploding with NAN values
                     
                    var fromParentBoneIndex = _fromSkeleton.GetParentBone(fromBoneIndex);
                    var targetParentBoneIndex = _targetSkeleton.GetParentBone(targetBoneIndex);

                    if (fromParentBoneIndex != -1 && targetParentBoneIndex != -1)
                    {
                        var toBone0 = _targetSkeleton.GetWorldTransform(targetBoneIndex);
                        var toBone1 = _targetSkeleton.GetWorldTransform(targetParentBoneIndex);
                        var targetBoneLength = Vector3.Distance(toBone0.Translation, toBone1.Translation);

                        var fromBone0 = _fromSkeleton.GetWorldTransform(fromBoneIndex);
                        var fromBone1 = _fromSkeleton.GetWorldTransform(fromParentBoneIndex);
                        var humanodBoneLength = Vector3.Distance(fromBone0.Translation, fromBone1.Translation);

                        if (RelativeScale.Value)
                            scale *= (targetBoneLength / humanodBoneLength);
                    }

                    if (scale <= 0)
                        scale = 0.00001f;   // To stop the calculations from exploding with NAN values

                    var parentWorld = Matrix.Identity;
                    if (fromParentBoneIndex != -1)
                        parentWorld = _fromSkeleton.GetAnimatedWorldTranform(fromParentBoneIndex);
                    var finalWorldPositon = desiredParentWorldTransform * Matrix.Invert(parentWorld);
                    finalWorldPositon.Decompose(out var _, out boneRotation, out bonePosition);
                    boneScale = scale;
                }
                else
                {
                    // Get the world position where we want to move the bone to
                    var targetBoneWorldTransform = _fromSkeleton.GetAnimatedWorldTranform(i);

                    // Apply the offset values
                    var desiredParentWorldTransform = MathUtil.CreateRotation(boneObject.BoneRotOffset) *
                        targetBoneWorldTransform *
                        Matrix.CreateTranslation(boneObject.BonePosOffset);

                    // Compute scaling
                    float scale = boneObject.BoneScaleOffset;
                    if (scale <= 0)
                        scale = 0.00001f;   // To stop the calculations from exploding with NAN values

                    var fromParentBoneIndex = _fromSkeleton.GetParentBone(i);

                    var parentWorld = Matrix.Identity;
                    if (fromParentBoneIndex != -1)
                        parentWorld = _fromSkeleton.GetAnimatedWorldTranform(fromParentBoneIndex);
                    var finalWorldPositon = desiredParentWorldTransform * Matrix.Invert(parentWorld);
                    finalWorldPositon.Decompose(out var _, out boneRotation, out bonePosition);
                    boneScale = scale;
                }

                _animationClip.DynamicFrames[0].Rotation[i] = boneRotation;
                _animationClip.DynamicFrames[0].Position[i] = bonePosition;
                _animationClip.DynamicFrames[0].Scale[i] *= new Vector3(boneScale);

                // Apply the inv scale to all children to avoid the mesh growing out of control
                var childBones = _fromSkeleton.GetChildBones(i);
                foreach (var childBoneIndex in childBones)
                {
                    float invScale = 1 / boneScale;
                    _animationClip.DynamicFrames[0].Scale[childBoneIndex] *= new Vector3(invScale);
                }

                _animationPlayer.Refresh();
            }
        }

        public override void OnMappingCreated(int humanoid01BoneIndex, int dwarfBoneIndex)
        {
            if (_targetSkeleton == null)
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
