using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Utility;

namespace AnimationEditor.PropCreator
{
    public class PropCreatorViewModel : BaseAnimationViewModel
    {
        public PropCreatorViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper) : base(pfs, skeletonHelper)
        {
            DisplayName  = "Anim.Prop Creator";
        }

        public override void Initialize()
        {
            var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "Prop", Color.Red, Scene));
            var editor = new PropCreatorEditorViewModel(propAsset, MainModelView.Data, ReferenceModelView.Data);
            Player.RegisterAsset(editor.Data);
            Editor = editor;
        }
    }
}
