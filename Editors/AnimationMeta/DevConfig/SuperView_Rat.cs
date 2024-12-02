using Editors.AnimationMeta.SuperView;
using Editors.Shared.Core.Common.BaseControl;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;
using Shared.GameFormats.AnimationPack;

namespace Editors.AnimationMeta.DevConfig
{
    internal class SuperView_Rat : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IPackFileService _packFileService;

        public SuperView_Rat(IEditorCreator editorCreator, IPackFileContainerLoader packFileContainerLoader, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileContainerLoader = packFileContainerLoader;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Throt.pack";
            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }

        public void OpenFileOnLoad()
        {
            CreateThrot(_editorCreator, _packFileService);
        }

        void CreateThrot(IEditorCreator creator, IPackFileService packfileService)
        {
            var debugInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            };

            var editor = creator.Create(EditorEnums.SuperView_Editor) as SuperViewViewModel;
            editor.Load(debugInput);
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
