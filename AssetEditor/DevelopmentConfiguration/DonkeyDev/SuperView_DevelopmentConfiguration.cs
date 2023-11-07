using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SuperView;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class SuperView_DevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public SuperView_DevelopmentConfiguration(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => false;
        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            CreateThrot(_editorCreator, _toolFactory, _packFileService);
        }

        static void CreateThrot(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<EditorHost<SuperViewViewModel>>();
            var debugInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            };
            editorView.Editor.SetDebugInputParameters(debugInput);
            creator.CreateEmptyEditor(editorView);
        }
    }

    /*
     *   public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_alarielle.variantmeshdefinition"),
                FragmentName = @"animations/animation_tables/hu1b_alarielle_staff_and_sword.frg",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("STAND")
            };
            //editorView.MainInput = new AnimationToolInput()
            //{
            //    Mesh = packfileService.FindFile(@"warmachines\engines\emp_steam_tank\emp_steam_tank01.rigid_model_v2"),
            //    FragmentName = @"animations/animation_tables/wm_steam_tank01.frg",
            //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
            //};
            //editorView.MainInput = new AnimationToolInput()
            //{
            //    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_state_troops_crossbowmen_ror.variantmeshdefinition"),
            //    FragmentName = @"animations/animation_tables/hu1_empire_sword_crossbow.frg",
            //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("FIRE_HIGH")
            //};

            creator.CreateEmptyEditor(editorView);
        }
     * 
      public static void CreateThrot(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            };

            creator.CreateEmptyEditor(editorView);
        }


        public static void CreatePlaguebearer(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\dae_plaguebearer_plagueridden.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu4d_wh3_nurgle_sword_on_palanquin.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("STAND_IDLE_2")
            };

            creator.CreateEmptyEditor(editorView);
        }
     */
}
