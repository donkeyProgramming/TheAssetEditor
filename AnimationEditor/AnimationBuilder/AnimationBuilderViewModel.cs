using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.Services;
using System.Collections.ObjectModel;
using View3D.Animation;

namespace AnimationEditor.AnimationBuilder
{
    public class AnimationNode
    { 
        public NotifyAttr<uint> ComputeOrder { get; set; }
        public NotifyAttr<string> Name { get; set; }

        public ObservableCollection<AnimationEditorItem> EditItems { get; set; } = new ObservableCollection<AnimationEditorItem>();

        AnimationClip _inputAnimation;
        AnimationClip _outputAnimation;
    }

    public class AnimationEditorItem
    {
        public NotifyAttr<bool> IsActive { get; set; }
        public NotifyAttr<bool> Name { get; set; }
        public NotifyAttr<bool> Description { get; set; }
    }


    public class AnimationBuilderViewModel : BaseAnimationViewModel
    {
        CopyPasteManager _copyPasteManager;

        public ObservableCollection<AnimationNode> AnimationNodes { get; set; } = new ObservableCollection<AnimationNode>();

        public AnimationBuilderViewModel(ToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, CopyPasteManager copyPasteManager) : base(toolFactory, pfs, skeletonHelper, "not_in_use1", "not_in_use2", false)
        {
            _copyPasteManager = copyPasteManager;
            DisplayName.Value = "Skeleton Editor";
            Pfs = pfs;
        }

        public PackFileService Pfs { get; }

        public override void Initialize()
        {
            MainModelView.IsControlVisible.Value = false;
            ReferenceModelView.IsControlVisible.Value = false;
            ReferenceModelView.Data.IsSelectable = false;

            var typedEditor = new Editor(_pfs, MainModelView.Data, Scene, _copyPasteManager);
            Editor = typedEditor;

            if (MainInput == null)
                MainInput = new AnimationToolInput();

            //typedEditor.Create(@"warmachines\chariot\grn_wolf_chariot\tech\grn_wolf_chariot_01.anim");
            typedEditor.CreateEditor(@"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\tech\nor_war_mammoth_howdah_01.anim");


            AnimationNodes.Add(new AnimationNode());





        }

        public static class AnimationBuilder_Debug
        {
            //public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
            //{
            //    var editorView = toolFactory.CreateEditorViewModel<TechSkeletonEditorViewModel>();
            //    editorView.MainInput = new AnimationToolInput()
            //    {
            //        Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_alarielle.variantmeshdefinition"),
            //        FragmentName = @"animations/animation_tables/hu1b_alarielle_staff_and_sword.frg",
            //        AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
            //    };
            //    editorView.MainInput = new AnimationToolInput()
            //    {
            //        Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
            //        FragmentName = @"animations/animation_tables/hu17_dlc16_throt.frg",
            //        AnimationSlot = AnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            //    };
            //
            //    //editorView.MainInput = new AnimationToolInput()
            //    //{
            //    //    Mesh = packfileService.FindFile(@"warmachines\engines\emp_steam_tank\emp_steam_tank01.rigid_model_v2"),
            //    //    FragmentName = @"animations/animation_tables/wm_steam_tank01.frg",
            //    //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
            //    //};
            //    //editorView.MainInput = new AnimationToolInput()
            //    //{
            //    //    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_state_troops_crossbowmen_ror.variantmeshdefinition"),
            //    //    FragmentName = @"animations/animation_tables/hu1_empire_sword_crossbow.frg",
            //    //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("FIRE_HIGH")
            //    //};
            //
            //    creator.CreateEmptyEditor(editorView);
            //}
        }
    }
}
