using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Utility;

namespace AnimationEditor.MountAnimationCreator
{

    public class MountAnimationCreatorViewModel : BaseAnimationViewModel
    {
        public MountAnimationCreatorViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper) : base(pfs, skeletonHelper)
        {
            DisplayName = "Anim.Prop Creator";
        }

        public override void Initialize()
        {
            //var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "Prop", Color.Red, Scene));
            //var editor = new PropCreatorEditorViewModel(propAsset, MainModelView.Data, ReferenceModelView.Data);
            //Player.RegisterAsset(editor.Data);
            Editor = new MountAnimationCreatorEditor();
        }
    }
}
