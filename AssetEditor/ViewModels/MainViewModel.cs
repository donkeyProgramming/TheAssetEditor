using AssetEditor.Views.Settings;
using CommonControls.BaseDialogs.ToolSelector;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    class MainViewModel : NotifyPropertyChangedImpl, IEditorCreator, IDropTarget<IEditorViewModel, bool>
    {
        ILogger _logger = Logging.Create<MainViewModel>();
        PackFileService _packfileService;
        public PackFileBrowserViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public ToolFactory ToolsFactory { get; set; }
        public ObservableCollection<IEditorViewModel> CurrentEditorsList { get; set; } = new ObservableCollection<IEditorViewModel>();

        int _selectedIndex;
        private bool _isClosingWithoutPrompt;

        public int SelectedEditorIndex { get => _selectedIndex; set => SetAndNotify(ref _selectedIndex, value); }

        public ICommand CloseToolCommand { get; set; }
        public ICommand CloseOtherToolsCommand { get; set; }

        public ICommand ClosingCommand { get; set; }

        public bool IsClosingWithoutPrompt
        {
            get => _isClosingWithoutPrompt;
            set
            {
                _isClosingWithoutPrompt = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand CloseAllToolsCommand { get; set; }
        public ICommand CloseToolsToRightCommand { get; set; }
        public ICommand CloseToolsToLeftCommand { get; set; }

        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, ToolFactory toolFactory, SchemaManager schemaManager, SkeletonAnimationLookUpHelper animationLookUpHelper)
        {
            _packfileService = packfileService;
            _packfileService.Database.BeforePackFileContainerRemoved += Database_BeforePackFileContainerRemoved;

            MenuBar = menuViewModel;
            MenuBar.EditorCreator = this;
            CloseToolCommand = new RelayCommand<IEditorViewModel>(CloseTool);
            CloseOtherToolsCommand = new RelayCommand<IEditorViewModel>(CloseOtherTools);
            ClosingCommand = new RelayCommand<IEditorViewModel>(Closing);
            CloseAllToolsCommand = new RelayCommand<IEditorViewModel>(CloseAllTools);
            CloseToolsToRightCommand = new RelayCommand<IEditorViewModel>(CloseToolsToRight);
            CloseToolsToLeftCommand = new RelayCommand<IEditorViewModel>(CloseToolsToLeft);

            FileTree = new PackFileBrowserViewModel(_packfileService);
            FileTree.ContextMenu = new DefaultContextMenuHandler(_packfileService, toolFactory, this);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = serviceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }

            //var anim3k = new BaseAnimationSlotHelper(GameTypeEnum.ThreeKingdoms).Values.Select(X => X.Value).ToList();
            //var animWh2 = new BaseAnimationSlotHelper(GameTypeEnum.Warhammer3).Values.Select(X => X.Value).ToList();
            //var iff = anim3k.Except(animWh2).ToList();
            //var iff2 = animWh2.Except(animWh2).ToList();
            //var dsfg = packfileService.Load(@"C:\Users\ole_k\Desktop\3k_animations.pack", false, true);
            //new BaseAnimationSlotHelper(GameTypeEnum.ThreeKingdoms).ExportAnimationDebugList(packfileService, @"3kanims");

            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
            {
                settingsService.CurrentSettings.CurrentGame = GameTypeEnum.Warhammer3;
                settingsService.CurrentSettings.SkipLoadingWemFiles = false;
                var gamePath = settingsService.GetGamePathForCurrentGame();
                if (gamePath != null)
                {
                    var gameName = GameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName;
                    var loadRes = _packfileService.LoadAllCaFiles(gamePath, gameName);
                    if (!loadRes)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                }
            }

            MetaDataTagDeSerializer.EnsureMappingTableCreated();

            if (settingsService.CurrentSettings.IsDeveloperRun)
            {
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
                //AnimationEditor.MountAnimationCreator.MountAnimationCreator_Debug.CreateLionAndHu01c(this, toolFactory, packfileService);
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
                OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\ksl\ksl_katarin\ksl_katarin_01.rigid_model_v2"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\nor_war_mammoth_warshrine_01.rigid_model_v2"));
                
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\bc1\tmb\tmb_warsphinx\tex\tmb_warsphinx_armour_01_base_colour.dds"));
                //OpenFile(packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2"));


                //AnimationPackEditor_Debug.Load(this, toolFactory, packfileService);

                //KitbashEditor_Debug.CreateSlayerHead(this, toolFactory, packfileService);

                //CreateEmptyEditor(editorView);


                CreateTestPackFiles(packfileService);
                //TexturePreviewController.CreateFromFilePath(@"C:\Users\ole_k\Desktop\TroyOrc.dds", _packfileService);
            }
        }

        private void Closing(IEditorViewModel editor)
        {
            if (!CurrentEditorsList.Any(editor => editor.HasUnsavedChanges) && !FileTree.Files.Any(node => node.UnsavedChanged))
            {
                IsClosingWithoutPrompt = true;
                return;
            }

            IsClosingWithoutPrompt = MessageBox.Show(
                "You have unsaved changes. Do you want to quit without saving?",
                "Quit Without Saving", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        void MemoryDebugging()
        {
            //while (true)
            //{
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //
            //    var window = ToolsFactory.CreateToolAsWindow<KitbasherEditor.ViewModels.KitbasherViewModel>(out var model);
            //    window.ShowDialog();
            //    window.DataContext = null;
            //    model.Close();
            //    model = null;
            //    window = null;
            //
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //}
        }


        void DebugCampaignBins(ApplicationSettingsService settingsService)
        {
            var game = GameTypeEnum.Warhammer2;
            var gName = GameInformationFactory.GetGameById(game).DisplayName;
            var gPath = settingsService.GetGamePathForGame(game);
            var gRes = _packfileService.LoadAllCaFiles(gPath, gName);

            //var allFiles = _packfileService.FindAllFilesInDirectory(@"animations/database/campaign/bin");
            var allFiles = _packfileService.FindAllFilesInDirectory(@"animations/campaign/database/bin");

            AnimationCampaignBinHelper.BatchProcess(allFiles);
        }

        private bool Database_BeforePackFileContainerRemoved(PackFileContainer container)
        {
            var openFiles = CurrentEditorsList
                .Where(x=> x.MainFile != null && _packfileService.GetPackFileContainer(x.MainFile) == container)
                .ToList();

            if (openFiles.Any())
            {
                if (MessageBox.Show("Closing pack file with open files, are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            foreach (var editor in openFiles)
            {
                CurrentEditorsList.Remove(editor);
                editor.Close();
            }

            return true;
        }

        void CreateTestPackFiles(PackFileService packfileService)
        {
            var newPackFile = packfileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            packfileService.SetEditablePack(newPackFile);
        }

        public void OpenFile(PackFile file)
        {
            if (file == null)
            {
                _logger.Here().Error($"Attempting to open file, but file is NULL");
                return;
            }

            var fileAlreadyAdded = CurrentEditorsList.FirstOrDefault(x => x.MainFile == file);
            if (fileAlreadyAdded != null)
            {
                SelectedEditorIndex = CurrentEditorsList.IndexOf(fileAlreadyAdded);
                _logger.Here().Information($"Attempting to open file '{file.Name}', but is is already open");
                return;
            }

           

            var fullFileName = _packfileService.GetFullPath(file );
            var allEditors = ToolsFactory.GetAllToolViewModelFromFileName(fullFileName);
            Type selectedEditor = null;
            if (allEditors.Count == 0)
            {
                _logger.Here().Warning($"Trying to open file {file.Name}, but there are no valid tools for it.");
                return;
            }
            else if (allEditors.Count == 1)
            {
                selectedEditor = allEditors.First().Type;
            }
            else
            {
                var selectedToolType = ToolSelectorWindow.CreateAndShow(allEditors.Select(x => x.EditorType));
                if (selectedToolType == EditorEnums.None)
                    return;
                selectedEditor = allEditors.First(x => x.EditorType == selectedToolType).Type;
            }

            var editorViewModel = ToolsFactory.CreateFromType(selectedEditor);

            _logger.Here().Information($"Opening {file.Name} with {editorViewModel.GetType().Name}");
            editorViewModel.MainFile = file;
            CurrentEditorsList.Add(editorViewModel);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }

        void CloseTool(IEditorViewModel tool)
        {
            if (tool.HasUnsavedChanges)
            {
                if (MessageBox.Show("Unsaved changed - Are you sure?", "Close", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var index = CurrentEditorsList.IndexOf(tool);
            CurrentEditorsList.RemoveAt(index);
            tool.Close();
        }

        void CloseOtherTools(IEditorViewModel tool)
        {
            foreach (var editorViewModel in CurrentEditorsList.ToList())
            {
                if (editorViewModel != tool)
                    CloseTool(editorViewModel);
            }
        }

        void CloseAllTools(IEditorViewModel tool)
        {
            foreach (var editorViewModel in CurrentEditorsList.ToList())
            {
                CloseTool(editorViewModel);
            }
        }

        void CloseToolsToLeft(IEditorViewModel tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (int i = index-1; i >= 0; i--)
            {
                CloseTool(CurrentEditorsList[0]);
            }
        }

        void CloseToolsToRight(IEditorViewModel tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (int i = CurrentEditorsList.Count-1; i > index; i--)
            {
                CloseTool(CurrentEditorsList[i]);
            }
        }

        public void CreateEmptyEditor(IEditorViewModel editorView)
        {
            CurrentEditorsList.Add(editorView);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }

        public bool AllowDrop(IEditorViewModel node, IEditorViewModel targeNode = default, bool insertAfterTargetNode = default)
        {
            return true;
        }

        public bool Drop(IEditorViewModel node, IEditorViewModel targeNode = default, bool insertAfterTargetNode = default)
        {
            var nodeIndex = CurrentEditorsList.IndexOf(node);
            var targetNodeIndex = CurrentEditorsList.IndexOf(targeNode);

            if (Math.Abs(nodeIndex - targetNodeIndex) == 1) // if tabs next to each other switch places
            {
                (CurrentEditorsList[nodeIndex], CurrentEditorsList[targetNodeIndex]) = (CurrentEditorsList[targetNodeIndex], CurrentEditorsList[nodeIndex]);
            }
            else // if tabs are not next to each other decide based on insertAfterTargetNode
            {
                if (insertAfterTargetNode)
                    targetNodeIndex += 1;

                var item = CurrentEditorsList[nodeIndex];

                CurrentEditorsList.RemoveAt(nodeIndex);

                if (targetNodeIndex > nodeIndex)
                    targetNodeIndex--;

                CurrentEditorsList.Insert(targetNodeIndex, item);
            }


            SelectedEditorIndex = CurrentEditorsList.IndexOf(node);
            return true;
        }
    }

}
