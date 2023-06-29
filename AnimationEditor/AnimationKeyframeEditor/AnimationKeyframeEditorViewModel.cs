using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Gizmo;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : BaseAnimationViewModel
    {
        public AnimationKeyframeEditorViewModel(ToolFactory toolFactory, PackFileService pfs, 
                                               SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
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
