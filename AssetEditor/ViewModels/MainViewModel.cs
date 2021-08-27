using AnimationEditor.SuperView;
using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using FileTypes.AnimationPack;
using FileTypes.DB;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace AssetEditor.ViewModels
{
    class MainViewModel : NotifyPropertyChangedImpl, IEditorCreator
    {
        ILogger _logger = Logging.Create<MainViewModel>();
        PackFileService _packfileService;
        public PackFileBrowserViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public ToolFactory ToolsFactory { get; set; }
        public ObservableCollection<IEditorViewModel> CurrentEditorsList { get; set; } = new ObservableCollection<IEditorViewModel>();

        int _selectedIndex;
        public int SelectedEditorIndex { get => _selectedIndex; set => SetAndNotify(ref _selectedIndex, value); }

        public ICommand CloseToolCommand { get; set; }

        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService, ToolFactory toolFactory, SchemaManager schemaManager)
        {
            _packfileService = packfileService;
            _packfileService.Database.BeforePackFileContainerRemoved += Database_BeforePackFileContainerRemoved;

            MenuBar = menuViewModel;
            MenuBar.EditorCreator = this;
            CloseToolCommand = new RelayCommand<IEditorViewModel>(CloseTool);
           
            FileTree = new PackFileBrowserViewModel(_packfileService);
            FileTree.ContextMenu = new DefaultContextMenuHandler(_packfileService, toolFactory, this);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            //DebugCampaignBins(settingsService);


            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = serviceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }

            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
            {
                //settingsService.CurrentSettings.CurrentGame = GameTypeEnum.ThreeKingdoms;
                var gamePath = settingsService.GetGamePathForCurrentGame();
                if (gamePath != null)
                {
                    var gameName = GameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName;
                    var loadRes = _packfileService.LoadAllCaFiles(gamePath, gameName);
                    if (!loadRes)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                    
                    //new AnimMetaBatchProcessor().BatchProcess(_packfileService, schemaManager, gameName);
                }
            }
          
            if (settingsService.CurrentSettings.IsDeveloperRun)
            {
                _packfileService.Load(@"C:\Users\ole_k\AssetEditor\MyStuff\ratcar.pack", true);

                //AnimMetaBatchProcessor processor = new AnimMetaBatchProcessor();
                //processor.BatchProcess(_packfileService, schemaManager, "Warhammer");

                SuperViewViewModel_Debug.CreateDamselEditor(this, toolFactory, packfileService);
                //CampaignAnimationCreator_Debug.CreateDamselEditor(this, toolFactory, packfileService);
                //MountAnimationCreator_Debug.CreateRaptorAndHu01d(this, toolFactory, packfileService);
                //KitbashEditor_Debug.CreatePaladin(this, toolFactory, packfileService);
                //AnimationTransferTool_Debug.CreateDamselEditor(this, toolFactory, packfileService);

                //var f = packfileService.FindFile(@"animations\campaign\database\bin\cam_hero_hu1d_def_spear_and_shield.bin");
                //OpenFile(f);



                //var f = packfileService.FindFile(@"animations\battle\persistent\hr1_warhorse_persistent_metadata_alive_0.anm.meta");
                //OpenFile(f);

                //AnimationPackEditor_Debug.Load(this, toolFactory, packfileService);

                //KitbashEditor_Debug.CreatePaladin(this, toolFactory, packfileService);

                //CreateEmptyEditor(editorView);

               // CreateTestPackFiles(packfileService);
            }
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

        public void OpenFile(IPackFile file)
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

            var fullFileName = _packfileService.GetFullPath(file as PackFile);
            var editorViewModel = ToolsFactory.GetToolViewModelFromFileName(fullFileName);
            if (editorViewModel == null)
            {
                _logger.Here().Warning($"Trying to open file {file.Name}, but there are no valid tools for it.");
                return;
            }

            _logger.Here().Information($"Opening {file.Name} with {editorViewModel.GetType().Name}");
            editorViewModel.MainFile = file;
            CurrentEditorsList.Add(editorViewModel);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }

        void CloseTool(IEditorViewModel tool)
        {
            if (tool.HasUnsavedChanges())
            {
                if (MessageBox.Show("Unsaved changed - Are you sure?", "Close", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var index = CurrentEditorsList.IndexOf(tool);
            CurrentEditorsList.RemoveAt(index);
            tool.Close();
        }

        public void CreateEmptyEditor(IEditorViewModel editorView)
        {
            CurrentEditorsList.Add(editorView);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }
    }
}
