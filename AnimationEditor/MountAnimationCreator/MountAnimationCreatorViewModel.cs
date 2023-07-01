using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using View3D.Animation.MetaData;
using View3D.Scene;

namespace AnimationEditor.MountAnimationCreator
{

    public class MountAnimationCreatorViewModel : BaseAnimationViewModel
    {
        private readonly AssetViewModelBuilder _assetViewModelBuilder;

        public MountAnimationCreatorViewModel(EventHub eventHub, MetaDataFactory metaDataFactory, AssetViewModelBuilder assetViewModelBuilder, MainScene scene, IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
            : base(eventHub, metaDataFactory, assetViewModelBuilder, scene, toolFactory, pfs, skeletonHelper, applicationSettingsService)
        {
            Set("Rider", "Mount", true);
            DisplayName.Value = "MountAnimCreator";
            _assetViewModelBuilder = assetViewModelBuilder;
        }

        public override void Initialize()
        {
            ReferenceModelView.Value.Data.IsSelectable = true;
            var propAsset = _assetViewModelBuilder.CreateAsset("New Anim", Color.Red);
            Player.RegisterAsset(propAsset);
            Editor = new Editor(_pfs, _skeletonHelper, MainModelView.Value.Data, ReferenceModelView.Value.Data, propAsset, Scene, _applicationSettingsService);
        }
    }

    public static class MountAnimationCreator_Debug
    {
        public static void CreateDamselAndGrymgoreEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\lzd_carnosaur_grymloq.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }


        public static void CreateKarlAndSquigEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_ch_karl.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_great_cave_squig.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateBroodHorrorEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_plague_priest.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_brood_horror.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateLionAndHu01b(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateLionAndHu01c(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\chs_marauder_horsemen.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateRaptorAndHu01b(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateRaptorAndHu01d(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_archer_armoured.variantmeshdefinition"),
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition"),
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateRaptorAndHu02(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_savage_orc_base.variantmeshdefinition") ,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition") ,
            };

            creator.CreateEmptyEditor(editorView);
        }


        public static void CreateRome2WolfRider(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\_variantmodels\man\skin\barb_base_full.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\rome2\riders\horse_rider\cycles\rider\horse_rider_walk.anim"),
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\wf1\grn\grn_giant_wolf\grn_giant_wolf_1.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\battle\wolf01\locomotion\wf1_walk_01.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateRome2WolfRiderAttack(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\_variantmodels\man\skin\barb_base_full.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\rome2\riders\horse_rider\attack\rider\sws_rider_attack_01.anim"),
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\wf1\grn\grn_giant_wolf\grn_giant_wolf_1.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\battle\wolf01\attacks\wf1_attack_01.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }


    }
}



