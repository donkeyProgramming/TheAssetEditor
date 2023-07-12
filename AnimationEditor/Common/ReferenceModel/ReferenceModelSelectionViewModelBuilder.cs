using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.PropCreator.ViewModels;
using AnimationMeta.Visualisation;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;
using System;

namespace AnimationEditor.Common.ReferenceModel
{
    public class ReferenceModelSelectionViewModelBuilder
    {
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly MetaDataFactory _metaDataFactory;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public ReferenceModelSelectionViewModelBuilder(AnimationPlayerViewModel animationPlayerViewModel, MetaDataFactory metaDataFactory, AssetViewModelBuilder assetViewModelBuilder,
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

        public ReferenceModelSelectionViewModel CreateEmpty()
        {
            var mainAsset = _assetViewModelBuilder.CreateAsset("Not in use" + Guid.NewGuid(), Color.Purple);
            var returnObj = new ReferenceModelSelectionViewModel(_metaDataFactory, _toolFactory, _pfs, mainAsset, "Not in use", _assetViewModelBuilder, _skeletonHelper, _applicationSettingsService);
            returnObj.IsControlVisible.Value = false;
            returnObj.Data.IsSelectable = false;
            return returnObj;
        }

        public ReferenceModelSelectionViewModel CreateAsset(bool createByDefault, string header, Color skeletonColour, AnimationToolInput input)
        {
            var mainAsset = _assetViewModelBuilder.CreateAsset(header, skeletonColour);
            var returnObj = new ReferenceModelSelectionViewModel(_metaDataFactory, _toolFactory, _pfs, mainAsset, header + ":", _assetViewModelBuilder, _skeletonHelper, _applicationSettingsService);

            if (createByDefault)
            {
                _animationPlayerViewModel.RegisterAsset(returnObj.Data);

                if (input != null)
                {
                    _assetViewModelBuilder.SetMesh(mainAsset, input.Mesh);
                    if (input.Animation != null)
                        _assetViewModelBuilder.SetAnimation(returnObj.Data, _skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));
                }
            }
            return returnObj;
        }
    }
}
