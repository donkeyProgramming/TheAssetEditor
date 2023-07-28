using System.Linq;
using System.Windows;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using View3D.Animation;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class CampaignAnimationCreatorViewModel : NotifyPropertyChangedImpl, IHostedEditor<CampaignAnimationCreatorViewModel>
    {
        AnimationToolInput _debugDataToLoad;
        SceneObject _selectedUnit;
        AnimationClip _selectedAnimationClip;

        private readonly SceneObjectBuilder _assetViewModelEditor;
        private readonly PackFileService _packFileService;
        private readonly SceneObjectViewModelBuilder _referenceModelSelectionViewModelBuilder;

        public FilterCollection<SkeletonBoneNode> ModelBoneList { get; set; } = new FilterCollection<SkeletonBoneNode>(null);
        public string EditorName => "Campaign Animation Creator";

        public CampaignAnimationCreatorViewModel(SceneObjectBuilder assetViewModelEditor, PackFileService pfs,  SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _packFileService = pfs;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
        }

        public void SetDebugInputParameters(AnimationToolInput debugDataToLoad)
        {
            _debugDataToLoad = debugDataToLoad;
        }

        public void Initialize(EditorHost<CampaignAnimationCreatorViewModel> owner)
        {
            var item = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "model", Color.Black, _debugDataToLoad);
            Create(item.Data);
            owner.SceneObjects.Add(item);
        }

        void Create(SceneObject rider)
        {
            _selectedUnit = rider;
            _selectedUnit.SkeletonChanged += SkeletonChanged;
            _selectedUnit.AnimationChanged += AnimationChanged;

            SkeletonChanged(_selectedUnit.Skeleton);
            AnimationChanged(_selectedUnit.AnimationClip);
        }

        public void SaveAnimation()
        {
            var animFile = _selectedUnit.AnimationClip.ConvertToFileFormat(_selectedUnit.Skeleton);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            SaveHelper.SaveAs(_packFileService, bytes, ".anim");
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
            for (var frameIndex = 0; frameIndex < newAnimation.DynamicFrames.Count; frameIndex++)
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
