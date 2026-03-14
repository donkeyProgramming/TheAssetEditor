using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Shared.Core.Common
{
    public class SceneObjectViewModelBuilder
    {
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly SceneObjectEditor _sceneObjectEditor;
        private readonly IPackFileService _pfs;
        private readonly ISkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IStandardDialogs _packFileUiProvider;

        public SceneObjectViewModelBuilder(
            AnimationPlayerViewModel animationPlayerViewModel, 
            SceneObjectEditor assetViewModelBuilder,
            IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonHelper, 
            IUiCommandFactory uiCommandFactory,
            IStandardDialogs packFileUiProvider)
        {
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectEditor = assetViewModelBuilder;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _uiCommandFactory = uiCommandFactory;
            _packFileUiProvider = packFileUiProvider;
        }

        public SceneObjectViewModel CreateAsset(string uniqeId, bool createByDefault, string header, Color skeletonColour, AnimationToolInput input)
        {
            var mainAsset = _sceneObjectEditor.CreateAsset(uniqeId, header, skeletonColour);
            var returnObj = new SceneObjectViewModel(_uiCommandFactory, _pfs, _packFileUiProvider, mainAsset, header + ":", _sceneObjectEditor, _skeletonHelper);

            if (createByDefault)
            {
                _animationPlayerViewModel.RegisterAsset(returnObj.Data);

                if (input != null)
                {
                    if (input.Mesh != null)
                        _sceneObjectEditor.SetMesh(mainAsset, input.Mesh);

                    if (input.Animation != null)
                    {
                        var animationPath = _pfs.GetFullPath(input.Animation);
                        _sceneObjectEditor.SetAnimation(mainAsset, animationPath);
                    }

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
