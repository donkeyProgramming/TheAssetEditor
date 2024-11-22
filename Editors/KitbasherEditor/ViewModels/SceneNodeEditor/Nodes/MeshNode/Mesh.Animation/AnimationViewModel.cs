using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Services;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Core.PackFiles;
using Shared.Ui.Common;
using Shared.Ui.Editors.BoneMapping;
using System.Windows;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2
{
    public partial class AnimationViewModel : ObservableObject, IDisposable
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SkeletonAnimationLookUpHelper _animLookUp;
        Rmv2MeshNode _meshNode;

        [ObservableProperty] string _skeletonName = string.Empty;
        [ObservableProperty] List<AnimatedBone> _animatedBones;
        [ObservableProperty] FilterCollection<AnimatedBone> _attachableBones = new(null);

        public AnimationViewModel(KitbasherRootScene kitbasherRootScene, SkeletonAnimationLookUpHelper animLookUp)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _animLookUp = animLookUp;
        }

        public void Initialize(Rmv2MeshNode meshNode)
        {
            _meshNode = meshNode;

            SkeletonName = _meshNode.Geometry.SkeletonName;

            var skeletonFile = _animLookUp.GetSkeletonFileFromName(SkeletonName);
            var bones = _meshNode.Geometry.GetUniqeBlendIndices();

            if (skeletonFile == null)
                SkeletonName = SkeletonName + "[MISSING from packs]";

            // Make sure the bones are valid, mapping can cause issues! 
            if (bones.Count != 0 && skeletonFile != null)
            {
                var activeBonesMin = bones.Min(x => x);
                var activeBonesMax = bones.Max(x => x);
                var skeletonBonesMax = skeletonFile.Bones.Max(x => x.Id);

                var hasValidBoneMapping = activeBonesMin >= 0 && skeletonBonesMax >= activeBonesMax;
                if (!hasValidBoneMapping)
                    MessageBox.Show("Mesh an invalid bones, this might cause issues. Its a result of an invalid re-rigging");

                var boneList = AnimatedBoneHelper.CreateFlatSkeletonList(skeletonFile);

                if (skeletonFile != null && hasValidBoneMapping)
                {
                    AnimatedBones = boneList
                        .OrderBy(x => x.BoneIndex.Value)
                        .ToList();
                }
            }

            var existingSkeletonMeshNode = _meshNode.GetParentModel();
            var existingSkeltonName = existingSkeletonMeshNode.GetMeshNode(0,0).Geometry.SkeletonName;
            var existingSkeletonFile = _animLookUp.GetSkeletonFileFromName(existingSkeltonName);
            if (existingSkeletonFile != null)
                AttachableBones.UpdatePossibleValues(AnimatedBoneHelper.CreateFlatSkeletonList(existingSkeletonFile), new AnimatedBone(-1, "none"));

            AttachableBones.SelectedItemChanged += ModelBoneList_SelectedItemChanged;
            AttachableBones.SearchFilter = (value, rx) => { return rx.Match(value.Name.Value).Success; };
            AttachableBones.SelectedItem = AttachableBones.PossibleValues.FirstOrDefault(x => x.Name.Value == _meshNode.AttachmentPointName);
        }

        private void ModelBoneList_SelectedItemChanged(AnimatedBone newValue)
        {
            var mainNode = _meshNode.GetParentModel() as MainEditableNode;
            if (mainNode == null)
                return;

            if (newValue != null && newValue.BoneIndex.Value != -1)
            {
                _meshNode.AttachmentPointName = newValue.Name.Value;
                _meshNode.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(_kitbasherRootScene, newValue.BoneIndex.Value);
            }
            else
            {
                _meshNode.AttachmentPointName = null;
                _meshNode.AttachmentBoneResolver = null;
            }
        }

        public void Dispose()
        {
            AttachableBones.SelectedItemChanged -= ModelBoneList_SelectedItemChanged;
        }

    }
}
