using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    public class MeshFitterViewModel : BoneMappingViewModel, IDisposable
    {
        private const int PreviewFrameIndex = 0;

        private readonly CommandFactory _commandFactory;
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly SceneManager _sceneManager;
        private bool _disposed;
        private GameSkeleton _targetSkeleton = null!;
        private GameSkeleton _fromSkeleton = null!;

        private AnimationClip _animationClip = null!;
        private AnimationPlayer _animationPlayer = null!;
        private AnimationPlayer _oldAnimationPlayer = null!;
        private List<Rmv2MeshNode> _meshNodes = null!;
        private SkeletonNode _currentSkeletonNode = null!;

        public NotifyAttr<bool> RelativeScale { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel ScaleFactor { get; set; } = new DoubleViewModel(1);

        public Vector3ViewModel SkeletonDisplayOffset { get; set; } = new Vector3ViewModel(0, 0, 0);

        public NotifyAttr<bool> IsBoneSelected { get; set; } = new NotifyAttr<bool>(false);
        public DoubleViewModel BoneScaleFactor { get; set; } = new DoubleViewModel(1);
        public Vector3ViewModel BonePositionOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel BoneRotationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);

        public MeshFitterViewModel(CommandFactory commandFactory, AnimationsContainerComponent animationsContainerComponent, SceneManager sceneManager, LocalizationManager localizationManager) : base(localizationManager)
        {
            _commandFactory = commandFactory;
            _animationsContainerComponent = animationsContainerComponent;
            _sceneManager = sceneManager;
        }

        public void Initialize(RemappedAnimatedBoneConfiguration configuration, List<Rmv2MeshNode> meshNodes, GameSkeleton targetSkeleton, AnimationFile currentSkeletonFile)
        {
            ShowApplyButton.Value = false;
            ShowTransformSection.Value = true;
            Initialize(configuration);

            _meshNodes = meshNodes;
            _targetSkeleton = targetSkeleton;

            ScaleFactor.PropertyChanged += (_0, _1) => ApplyMeshFittingTransforms();
            BoneScaleFactor.PropertyChanged += (_0, _1) => BoneScaleUpdate((float)BoneScaleFactor.Value, MeshBones.SelectedItem);
            BonePositionOffset.OnValueChanged += (viewModel) => BonePositionUpdated(viewModel, MeshBones.SelectedItem);
            BoneRotationOffset.OnValueChanged += (viewModel) => BoneRotationUpdated(viewModel, MeshBones.SelectedItem);
            SkeletonDisplayOffset.OnValueChanged += (viewModel) => SkeletonDisplayOffsetUpdated(viewModel);
            RelativeScale.PropertyChanged += (_0, _1) => ApplyMeshFittingTransforms();
            MeshBones.SelectedItemChanged += _ => OnBoneSelected();

            _animationPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), "Temp animation rerig" + Guid.NewGuid());
            _fromSkeleton = new GameSkeleton(currentSkeletonFile, _animationPlayer);

            // Build empty animation
            _animationClip = new AnimationClip();
            _animationClip.DynamicFrames.Add(new AnimationClip.KeyFrame());

            for (var i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[PreviewFrameIndex].Rotation.Add(_fromSkeleton.Rotation[i]);
                _animationClip.DynamicFrames[PreviewFrameIndex].Position.Add(_fromSkeleton.Translation[i]);
                _animationClip.DynamicFrames[PreviewFrameIndex].Scale.Add(Vector3.One);
            }

            _animationPlayer.SetAnimation(_animationClip, _fromSkeleton);
            _animationPlayer.Play();

            _currentSkeletonNode = new SkeletonNode(_fromSkeleton);
            _currentSkeletonNode.SelectedNodeColour = Color.White;
            _currentSkeletonNode.NodeColour = Color.Red;
            _sceneManager.RootNode.AddObject(_currentSkeletonNode);

            _oldAnimationPlayer = _meshNodes.First().AnimationPlayer!;
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _animationPlayer;
        }

        protected override void MappingUpdated()
        {
            if (_targetSkeleton == null)
                return;

            ApplyMeshFittingTransforms();
        }

        private void SkeletonDisplayOffsetUpdated(Vector3ViewModel viewModel)
        {
            _currentSkeletonNode.ModelMatrix = Matrix.CreateTranslation((float)viewModel.X.Value, (float)viewModel.Y.Value, (float)viewModel.Z.Value);
        }

        private void OnBoneSelected()
        {
            var selectedBone = MeshBones.SelectedItem;
            IsBoneSelected.Value = selectedBone != null;

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            if (selectedBone != null)
            {
                BoneScaleFactor.Value = selectedBone.BoneScaleOffset;
                BoneRotationOffset.Set(selectedBone.BoneRotOffset.X, selectedBone.BoneRotOffset.Y, selectedBone.BoneRotOffset.Z);
                BonePositionOffset.Set(selectedBone.BonePosOffset.X, selectedBone.BonePosOffset.Y, selectedBone.BonePosOffset.Z);

                _currentSkeletonNode.SelectedBoneIndex = selectedBone.BoneIndex.Value;
                rootNode.SkeletonNode.SelectedBoneIndex = selectedBone.MappedBoneIndex.Value;
            }
            else
            {
                BoneScaleFactor.Value = 1;
                BoneRotationOffset.Set(0);
                BonePositionOffset.Set(0);

                _currentSkeletonNode.SelectedBoneIndex = null;
                rootNode.SkeletonNode.SelectedBoneIndex = null;
            }
        }

        private void BoneScaleUpdate(float newValue, AnimatedBone? bone)
        {
            if (bone == null)
                return;
            bone.BoneScaleOffset = newValue;
            ApplyMeshFittingTransforms();
        }

        private void BoneRotationUpdated(Vector3ViewModel newValue, AnimatedBone? bone)
        {
            if (bone == null)
                return;
            bone.BoneRotOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ApplyMeshFittingTransforms();
        }

        private void BonePositionUpdated(Vector3ViewModel newValue, AnimatedBone? bone)
        {
            if (bone == null)
                return;
            bone.BonePosOffset = new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value);
            ApplyMeshFittingTransforms();
        }

        private void ApplyMeshFittingTransforms()
        {
            var sourceBones = MeshBones.PossibleValues.First();

            // Rebuild the mapping index as its easy to work with
            var mapping = AnimatedBoneHelper.BuildRemappingList(sourceBones);

            // Reset the animation back to bind pose
            for (var i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[PreviewFrameIndex].Rotation[i] = _fromSkeleton.Rotation[i];
                _animationClip.DynamicFrames[PreviewFrameIndex].Position[i] = _fromSkeleton.Translation[i];
                _animationClip.DynamicFrames[PreviewFrameIndex].Scale[i] = Vector3.One;
            }

            // Set the base scale for the mesh and apply the animation
            var baseScale = (float)ScaleFactor.Value;
            _animationClip.DynamicFrames[PreviewFrameIndex].Scale[0] = new Vector3(baseScale);
            _animationPlayer!.Refresh();

            if (baseScale == 0)
                return;

            for (var i = 0; i < _fromSkeleton.BoneCount; i++)
            {
                var mappedIndex = mapping.FirstOrDefault(x => x.OriginalValue == i);
                var boneValuesObject = sourceBones.GetFromBoneId(i);

                var fromBoneIndex = i;
                var fromParentBoneIndex = _fromSkeleton.GetParentBoneIndex(fromBoneIndex);

                // Get the world position where we want to move the bone to
                var desiredBonePosWorld = mappedIndex != null
                    ? _targetSkeleton.GetAnimatedWorldTranform(mappedIndex.NewValue)
                    : _fromSkeleton.GetAnimatedWorldTranform(i);

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
                    var targetParentBoneIndex = _targetSkeleton!.GetParentBoneIndex(targetBoneIndex);

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
                var scale = boneValuesObject.BoneScaleOffset * relativeScale;

                // To stop the calculations from exploding with NAN values
                if (scale <= 0 || float.IsNaN(scale))
                    scale = 0.00001f;

                var parentWorld = Matrix.Identity;
                if (fromParentBoneIndex != -1)
                    parentWorld = _fromSkeleton.GetAnimatedWorldTranform(fromParentBoneIndex);
                var bonePositionLocalSpace = desiredBonePosWorldWithOffsets * Matrix.Invert(parentWorld);
                bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                // Apply the values to the animation
                _animationClip.DynamicFrames[PreviewFrameIndex].Rotation[i] = boneRotation;
                _animationClip.DynamicFrames[PreviewFrameIndex].Position[i] = bonePosition;
                _animationClip.DynamicFrames[PreviewFrameIndex].Scale[i] *= new Vector3(scale);

                // Apply the inv scale to all children to avoid the mesh growing out of control
                var childBones = _fromSkeleton.GetDirectChildBones(i);
                foreach (var childBoneIndex in childBones)
                {
                    var invScale = 1 / scale;
                    _animationClip.DynamicFrames[PreviewFrameIndex].Scale[childBoneIndex] *= new Vector3(invScale);
                }

                _animationPlayer.Refresh();
            }
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

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // Restore animation player
            _animationsContainerComponent.Remove(_animationPlayer);
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _oldAnimationPlayer;

            // Remove the skeleton node
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            rootNode.SkeletonNode.SelectedBoneIndex = null;
            _sceneManager.RootNode.RemoveObject(_currentSkeletonNode);
        }


        protected override void ApplyChanges()
        {
            var frame = AnimationSampler.Sample(PreviewFrameIndex, _fromSkeleton, _animationClip);
            _commandFactory.Create<CreateAnimatedMeshPoseCommand>().Configure(x => x.Configure(_meshNodes, frame)).BuildAndExecute();
        }
    }
}
