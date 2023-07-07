using AssetEditor.UiCommands;
using CommonControls.Events.UiCommands;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace AssetEditor
{
    public class DevelopmentConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public DevelopmentConfiguration(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void CreateTestPackFiles()
        {
            var newPackFile = _packFileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            _packFileService.SetEditablePack(newPackFile);
        }

        public void OpenFileOnLoad()
        {     
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }

        /*
                     if (settingsService.CurrentSettings.IsDeveloperRun)
            {
                CreateTestPackFiles(packfileService);
                //AudioTool_Debug.CreateOvnCompilerProject(packfileService);
                //AnimationEditor.MountAnimationCreator.MountAnimationCreator_Debug.CreateLionAndHu01c(this, toolFactory, packfileService);




                //new BaseAnimationSlotHelper(GameTypeEnum.Warhammer2).ExportAnimationDebugList(packfileService, @"c:\temp\3kanims.txt");

                //DefaultAnimationSlotTypeHelper.ExportAnimationDebugList(packfileService);

                //var reportService = new Report.FileListReportGenerator(packfileService, settingsService);
                //var comparePath = reportService.Create();
                //
                //reportService.CompareFiles(@"C:\Users\ole_k\AssetEditor\Reports\FileList\Warhammer III 1.2.0.0 PackFiles.csv", @"C:\Users\ole_k\AssetEditor\Reports\FileList\Warhammer III 1.3.0.0 Packfiles.csv");

                //;
                //AnimationEditor.AnimationTransferTool.AnimationTransferTool_Debug.CreateDwardAndEmpArcher(this, toolFactory, packfileService);

                //var r = new Rmv2Information(_packfileService);
                //r.Create(GameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName);

                //var soundEditor = new CommonControls.Editors.Sound.SoundEditor(packfileService);
                //soundEditor.CreateSoundMap();

                //var s = new AnimMetaDataReportGenerator(_packfileService);
                //s.Create(GameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName);
                //
                //OpenFile(packfileService.FindFile(@"terrain\campaigns\wh2_main_great_vortex_map_1\global_meshes\land_mesh_20.rigid_model_v2"));
                //CommonControls.FormatResearch.TerrainRmv2Decoder.CreateTerrainCustom(_packfileService);
                //OpenFile(packfileService.FindFile(@"terrain\tiles\campaign\dwarf_custom\86x57_karaz_a_karak\custom_mesh.rigid_model_v2"));

                //_packfileService.Load(@"C:\Users\ole_k\AssetEditor\MyStuff\TroyBmdFile.pack");
                //new FastBinParser().ParseFile(_packfileService.FindFile(@"sky_troy_generic_01.bmd.bmd"));
                //new FastBinParser().ParseFile(_packfileService.FindFile(@"troy_siege_model_01.bmd"));
                //new FastBinParser().ParseFile(_packfileService.FindFile(@"prefabs\campaign\empire_mountain_fort.bmd"));

                //

                //var invMatrixPackFile = _packfileService.FindFile(@"animations\skeletons\advisorcrow01.bone_inv_trans_mats");
                //
                //var skeletonFile = _packfileService.FindFile(@"animations\skeletons\humanoid01.anim");
                //var skeletonAnim = AnimationFile.Create(skeletonFile);
                //var gameSkeleton = new GameSkeleton(skeletonAnim, null);
                //var invTest = gameSkeleton.CreateInvMatrixFile();
                //
                //var bytes = invTest.GetBytes();
                //var reloadedInv = AnimInvMatrixFile.Create(new Filetypes.ByteParsing.ByteChunk(bytes));
                //
                //var originalInvFile = _packfileService.FindFile(@"animations\skeletons\humanoid01.bone_inv_trans_mats");
                //var originalInv = AnimInvMatrixFile.Create(originalInvFile.DataSource.ReadDataAsChunk());
                //
                //var reloadedInstance = new CommonControls.Editors.AnimationFilePreviewEditor.InvMatrixToTextConverter();
                //var orgText = "Org\n" + reloadedInstance.GetText(originalInvFile.DataSource.ReadData());
                //var reloadedText = "Reloaded\n" + reloadedInstance.GetText(bytes);
                //
                //var t = AnimInvMatrixFile.Create(invMatrixPackFile.DataSource.ReadDataAsChunk());

                //_packfileService.Load(@"C:\Users\ole_k\AssetEditor\MyStuff\ratcar.pack", true);

                //AnimMetaBatchProcessor processor = new AnimMetaBatchProcessor();
                //processor.BatchProcess(_packfileService, schemaManager, "Warhammer");

                //AnimationEditor.SuperView.SuperViewViewModel_Debug.CreateThrot(this, toolFactory, packfileService);
                //CampaignAnimationCreator_Debug.CreateDamselEditor(this, toolFactory, packfileService);


                //var gameName = GameInformationFactory.GetGameById(GameTypeEnum.Rome_2_Remastered).DisplayName;
                //var romePath = settingsService.GetGamePathForGame(GameTypeEnum.Rome_2_Remastered);
                //var loadRes = _packfileService.LoadAllCaFiles(romePath, gameName);
                ////
                //AnimationEditor.MountAnimationCreator.MountAnimationCreator_Debug.CreateRome2WolfRider(this, toolFactory, packfileService);

                //KitbashEditor_Debug.CreateLoremasterHead(this, toolFactory, packfileService);
                //AnimationEditor.AnimationTransferTool.AnimationTransferTool_Debug.CreateBowCentigor(this, toolFactory, packfileService);
                //AnimationEditor.AnimationTransferTool.AnimationTransferTool_Debug.CreateDamselEditor(this, toolFactory, packfileService);
                //var f = packfileService.FindFile(@"animations\campaign\database\bin\cam_hero_hu1d_def_spear_and_shield.bin");
                //
                //AnimationEditor.AnimationBuilder.AnimationBuilderViewModel.AnimationBuilder_Debug.CreateExampleAnimation(this, toolFactory, packfileService, animationLookUpHelper);


                //packfileService.DeepSearch("wh2_main_vor_deadwood_the_frozen_city", false);
                //packfileService.DeepSearch("31x11_dragonback_skew_mirror_01", false);
                //packfileService.DeepSearch("context_viewer", false);



                //AnimationPackLoader.Load(packfileService.FindFile(@"animations\animation_tables\animation_tables.animpack"));


                //var f = packfileService.FindFile(@"animations/matched_combat/attila_generated.bin");
                //new CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.MatachedAnimFile("sdasda", f.DataSource.ReadData());
                //
                // OpenFile(packfileService.FindFile(@"animations\database\battle\bin\animation_tables.animpack"));
                // OpenFile(packfileService.FindFile(@"animations\animation_tables\animation_tables.animpack"));

                //OpenFile(packfileService.FindFile(@"animations\database\battle\bin\animation_tables.animpack"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\ksl\ksl_katarin\ksl_katarin_cloth_cloak_01.rigid_model_v2"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\ksl\ksl_katarin\ksl_katarin_01.rigid_model_v2"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\nor_war_mammoth_warshrine_01.rigid_model_v2"));

                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\bc1\tmb\tmb_warsphinx\tex\tmb_warsphinx_armour_01_base_colour.dds"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2"));


                //AnimationPackEditor_Debug.Load(this, toolFactory, packfileService);

                //KitbashEditor_Debug.CreateSlayerHead(this, toolFactory, packfileService);

                //CreateEmptyEditor(editorView);



                //TexturePreviewController.CreateFromFilePath(@"C:\Users\ole_k\Desktop\TroyOrc.dds", _packfileService);
            }
         */



        //public bool OverrideLoadAllFiles { get; set; }

        // OpenFile()
        // SkipLoadingWems



    }
}
