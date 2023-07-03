using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Scene;

namespace AnimationEditor.MountAnimationCreator
{
    public class MountAnimationCreatorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;

        public MountAnimationCreatorViewModel(Editor editor,
            IComponentInserter componentInserter, 
            ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder, 
            AnimationPlayerViewModel animationPlayerViewModel, 
            EventHub eventHub,
            AssetViewModelBuilder assetViewModelBuilder, 
            MainScene scene)
            : base(componentInserter, animationPlayerViewModel, scene)
        {
            DisplayName.Value = "MountAnimCreator";
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _assetViewModelBuilder = assetViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Rider", Color.Black, MainInput);
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Mount", Color.Black, RefInput);

            ReferenceModelView.Value.Data.IsSelectable = true;

            var propAsset = _assetViewModelBuilder.CreateAsset("New Anim", Color.Red);
            Player.RegisterAsset(propAsset);
           
            Editor.Create(MainModelView.Value.Data, ReferenceModelView.Value.Data, propAsset);
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



