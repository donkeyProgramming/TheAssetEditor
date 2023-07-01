using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Windows;
using View3D.Animation;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class Editor : NotifyPropertyChangedImpl
    {
        public FilterCollection<SkeletonBoneNode> ModelBoneList { get; set; } = new FilterCollection<SkeletonBoneNode>(null);

        AssetViewModel _selectedUnit;
        private readonly AssetViewModelBuilder _assetViewModelEditor;
        PackFileService _pfs;
        AnimationClip _selectedAnimationClip;

        public Editor(AssetViewModelBuilder assetViewModelEditor, PackFileService pfs)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _pfs = pfs;
        

        }

        public Editor Create(AssetViewModel rider)
        {
            _selectedUnit = rider;
            _selectedUnit.SkeletonChanged += SkeletonChanged;
            _selectedUnit.AnimationChanged += AnimationChanged;

            SkeletonChanged(_selectedUnit.Skeleton);
            AnimationChanged(_selectedUnit.AnimationClip);

            return this;

        }

        public void SaveAnimation()
        {
            var animFile = _selectedUnit.AnimationClip.ConvertToFileFormat(_selectedUnit.Skeleton);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            SaveHelper.SaveAs(_pfs, bytes, ".anim");
        }

        public void Convert()
        {
            if (_selectedAnimationClip == null)
            {
                MessageBox.Show("No animation selected");
                return;
            }

            if (ModelBoneList.SelectedItem == null)
            {
                MessageBox.Show("No root bone selected");
                return;
            }

            var newAnimation = _selectedAnimationClip.Clone();

            for (int frameIndex = 0; frameIndex < newAnimation.DynamicFrames.Count; frameIndex++)
            {
                var frame = newAnimation.DynamicFrames[frameIndex];
                frame.Position[ModelBoneList.SelectedItem.BoneIndex] = Vector3.Zero;
                frame.Rotation[ModelBoneList.SelectedItem.BoneIndex] = Quaternion.Identity;
            }

            _selectedUnit.AnimationChanged -= AnimationChanged;
            _assetViewModelEditor.SetAnimationClip(_selectedUnit, newAnimation, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
            _selectedUnit.AnimationChanged += AnimationChanged;
        }

        private void AnimationChanged(AnimationClip newValue)
        {
            _selectedAnimationClip = newValue;
        }

        private void SkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ModelBoneList.UpdatePossibleValues(null);
            }
            else
            {
                ModelBoneList.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));
                ModelBoneList.SelectedItem = ModelBoneList.PossibleValues.FirstOrDefault(x => x.BoneName.ToLower() == "animroot");
            }
        }
    }
}
