using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.MountAnimationCreator
{
    public class MountAnimationCreatorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly SceneObjectBuilder _sceneObjectBuilder;

        AnimationToolInput _inputRiderData;
        AnimationToolInput _inputMountData;

        public override NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Mount Animation Creator");

        public MountAnimationCreatorViewModel(
            Editor editor,
            IComponentInserter componentInserter,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            SceneObjectBuilder sceneObjectBuilder,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            Editor = editor;
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _sceneObjectBuilder = sceneObjectBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }


        public void SetDebugInputParameters(AnimationToolInput rider, AnimationToolInput mount)
        {
            _inputRiderData = rider;
            _inputMountData = mount;
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var riderItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Rider", Color.Black, _inputRiderData);
            var mountItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Mount", Color.Black, _inputMountData);
            mountItem.Data.IsSelectable = true;

            var propAsset = _sceneObjectBuilder.CreateAsset("New Anim", Color.Red);
            Player.RegisterAsset(propAsset);

            Editor.Create(riderItem.Data, mountItem.Data, propAsset);
            SceneObjects.Add(riderItem);
            SceneObjects.Add(mountItem);
        }
    }

  /*  public static class MountAnimationCreator_Debug
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
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_savage_orc_base.variantmeshdefinition"),
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition"),
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


    }*/
}



