using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : BaseAnimationViewModel
    {
        public AnimationKeyframeEditorViewModel(ToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
            : base(toolFactory, pfs, skeletonHelper, applicationSettingsService, "Rider", "Mount")
        {
            DisplayName.Value = "AnimationKeyframeEditor";
        }

        public override void Initialize()
        {
            ReferenceModelView.Data.IsSelectable = true;
            var propAsset = Scene.AddComponent(new AssetViewModel(_pfs, "NewAnim", Color.Red, Scene, _applicationSettingsService));
            Player.RegisterAsset(propAsset);
            Editor = new Editor(_pfs, _skeletonHelper, MainModelView.Data, ReferenceModelView.Data, propAsset, Scene, _applicationSettingsService);
        }
    }
}
