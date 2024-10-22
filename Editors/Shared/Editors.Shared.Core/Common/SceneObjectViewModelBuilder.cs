using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.Shared.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.Shared.Core.Common
{
    public class SceneObjectViewModelBuilder
    {
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly IMetaDataFactory _metaDataFactory;
        private readonly SceneObjectEditor _assetViewModelBuilder;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IUiCommandFactory _uiCommandFactory;

        public SceneObjectViewModelBuilder(AnimationPlayerViewModel animationPlayerViewModel, IMetaDataFactory metaDataFactory, SceneObjectEditor assetViewModelBuilder,
            PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, IUiCommandFactory uiCommandFactory)
        {
            _animationPlayerViewModel = animationPlayerViewModel;
            _metaDataFactory = metaDataFactory;
            _assetViewModelBuilder = assetViewModelBuilder;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _uiCommandFactory = uiCommandFactory;
        }

        public SceneObjectViewModel CreateAsset(bool createByDefault, string header, Color skeletonColour, AnimationToolInput input, bool allowMetaData = false)
        {
            var mainAsset = _assetViewModelBuilder.CreateAsset(header, skeletonColour);
            var returnObj = new SceneObjectViewModel(_uiCommandFactory, _metaDataFactory, _pfs, mainAsset, header + ":", _assetViewModelBuilder, _skeletonHelper);
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
