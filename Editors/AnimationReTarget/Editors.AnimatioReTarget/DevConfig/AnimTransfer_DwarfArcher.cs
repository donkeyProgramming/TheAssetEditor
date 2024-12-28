using Editors.AnimatioReTarget.Editor;
using Editors.Shared.Core.Common.BaseControl;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;

namespace Editors.AnimatioReTarget.DevConfig
{
    internal class AnimTransfer_DwarfArcher : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileService _packFileService;

        public AnimTransfer_DwarfArcher(IEditorCreator editorCreator, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.ShowCAWemFiles = false;
        }

        public void OpenFileOnLoad()
        {
            CreateDwarfAndEmpArcher(_editorCreator, _packFileService);
        }

        static void CreateDwarfAndEmpArcher(IEditorCreator creator, IPackFileService packfileService)
        {
            var targetInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition")
            };

            var sourceInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_archer_ror.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\humanoid01\sword_and_pistol\missile_attacks\hu1_swp_missile_attack_aim_to_shootready_01.anim")
            };

            var editor = (AnimationRetargetEditor)creator.Create(EditorEnums.AnimationRetarget_Editor);
            editor.LoadData(targetInput, sourceInput);
        }
    }
}

/*
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
     */
