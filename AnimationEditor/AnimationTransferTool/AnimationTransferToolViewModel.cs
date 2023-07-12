using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.AnimationTransferTool
{
    public class AnimationTransferToolViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;

        public AnimationTransferToolViewModel(
            Editor editor,
            ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            IComponentInserter componentInserter,
            AssetViewModelBuilder assetViewModelBuilder,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            DisplayName.Value = "Animation transfer tool";
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _assetViewModelBuilder = assetViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Target", Color.Black, MainInput);
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(false, "Source", Color.Black, RefInput);

            ReferenceModelView.Value.Data.IsSelectable = false;
            var generatedAnimationAsset = _assetViewModelBuilder.CreateAsset("Generated", Color.Black);
            Player.RegisterAsset(generatedAnimationAsset);
            Editor.Create(MainModelView.Value.Data, ReferenceModelView.Value.Data, generatedAnimationAsset);
        }
    }

    public static class AnimationTransferTool_Debug
    {
        public static void CreateDwardAndEmpArcher(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<AnimationTransferToolViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\wef_bladesinger.variantmeshdefinition"),
                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\ogr_maneater_base.variantmeshdefinition"),
                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_savage_orc_base.variantmeshdefinition")
                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\chs_giants.variantmeshdefinition")
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition")
                //Animation = packfileService.FindFile(@"animations\battle\humanoid01b\sword_and_chalice\stand\hu1b_swch_stand_idle_04.anim")
                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_forest_goblins_base.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_archer_ror.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\humanoid01\sword_and_pistol\missile_attacks\hu1_swp_missile_attack_aim_to_shootready_01.anim")

                //Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\3k_dlc05_unit_metal_camp_crushers.variantmeshdefinition"),
                //Animation = packfileService.FindFile(@"animations\battle\character\male01\infantry\inf_1h095bow\special_ability\special_ability_arrow_storm_01\inf_1h095bow_arrow_storm_01.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateChaosSpawn(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<AnimationTransferToolViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\bst_chaos_spawn.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\bst_chaos_spawn.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\hybridcreature02\combat_idles\hc2_combat_idle_02.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateGreatEagle(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<AnimationTransferToolViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_great_eagle.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_great_eagle.variantmeshdefinition"),
                //Animation = packfileService.FindFile(@"animations\battle\hybridcreature02\combat_idles\hc2_combat_idle_02.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateBowCentigor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<AnimationTransferToolViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\bst_centigors_base.variantmeshdefinition")
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\subset\elves\bow\missile_actions\hu1b_bow_aim_01.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateFlyingSquig(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<AnimationTransferToolViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\rp2\grn\grn_squig_herd\grn_squig_herd_01_2b.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\battle\raptor02b\stand\cust_rp2_stand_idle_01.anim"),
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hr1\brt\brt_royal_pegasus\brt_pegasus_01.rigid_model_v2"),
                Animation = packfileService.FindFile(@"animations\battle\horse01\stand\hr1_stand_idle_05.anim")
            };

            creator.CreateEmptyEditor(editorView);
        }


    }
}
