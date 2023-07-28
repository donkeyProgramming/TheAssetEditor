using System.Linq;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.PropCreator.ViewModels;
using AnimationMeta.Visualisation;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SceneObjectViewModelBuilder
    {
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly MetaDataFactory _metaDataFactory;
        private readonly SceneObjectBuilder _assetViewModelBuilder;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneObjectViewModelBuilder(AnimationPlayerViewModel animationPlayerViewModel, MetaDataFactory metaDataFactory, SceneObjectBuilder assetViewModelBuilder,
            IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
        {
            _animationPlayerViewModel = animationPlayerViewModel;
            _metaDataFactory = metaDataFactory;
            _assetViewModelBuilder = assetViewModelBuilder;
            _toolFactory = toolFactory;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _applicationSettingsService = applicationSettingsService;
        }

        public SceneObjectViewModel CreateAsset(bool createByDefault, string header, Color skeletonColour, AnimationToolInput input, bool allowMetaData = false)
        {
            var mainAsset = _assetViewModelBuilder.CreateAsset(header, skeletonColour);
            var returnObj = new SceneObjectViewModel(_metaDataFactory, _toolFactory, _pfs, mainAsset, header + ":", _assetViewModelBuilder, _skeletonHelper, _applicationSettingsService);
            returnObj.AllowMetaData.Value = allowMetaData;

            if (createByDefault)
            {
                _animationPlayerViewModel.RegisterAsset(returnObj.Data);

                if (input != null)
                {
                    if (input.Mesh != null)
                        _assetViewModelBuilder.SetMesh(mainAsset, input.Mesh);

                    if (input.Animation != null)
                        _assetViewModelBuilder.SetAnimation(mainAsset, _skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));

                    if (input.FragmentName != null)
                    {
                        returnObj.FragAndSlotSelection.FragmentList.SelectedItem = returnObj.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == input.FragmentName);

                        if (input.AnimationSlot != null)
                            returnObj.FragAndSlotSelection.FragmentSlotList.SelectedItem = returnObj.FragAndSlotSelection.FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == input.AnimationSlot.Value);
                    }
                }
            }
            return returnObj;
        }
    }
}
