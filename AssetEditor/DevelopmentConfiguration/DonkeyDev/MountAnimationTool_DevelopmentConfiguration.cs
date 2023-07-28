using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class MountAnimationTool_DevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public MountAnimationTool_DevelopmentConfiguration(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => true;
        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            CreateLionAndHu01b(_editorCreator, _toolFactory, _packFileService);
        }

        static void CreateLionAndHu01b(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<EditorHost<MountAnimationCreatorViewModel>>();
            var riderInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
            };

            var mountInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
            };
            editorView.Editor.SetDebugInputParameters(riderInput, mountInput);
            creator.CreateEmptyEditor(editorView);
        }
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
