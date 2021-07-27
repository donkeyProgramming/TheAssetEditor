using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimMetaEditor;
using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls;
using CommonControls.Editors.AnimationPack;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CommonControls.Table;
using Filetypes.RigidModel;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
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

        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService, ToolFactory toolFactory)
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

            if (settingsService.CurrentSettings.IsFirstTimeStartingApplication)
            {
                var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.DataContext = serviceProvider.GetRequiredService<SettingsViewModel>();
                settingsWindow.ShowDialog();

                settingsService.CurrentSettings.IsFirstTimeStartingApplication = false;
                settingsService.Save();
            }


            // ----------
            var gName = GameInformationFactory.GetGameById(GameTypeEnum.Warhammer2).DisplayName;
            var gPath = settingsService.GetGamePathForGame(GameTypeEnum.Warhammer2);
            var gRes = _packfileService.LoadAllCaFiles(gPath, gName);

           // var allFiles = _packfileService.FindAllFilesInDirectory(@"animations/database/campaign/bin");
            var allFiles = _packfileService.FindAllFilesInDirectory(@"animations/campaign/database/bin");

            AnimationCampaignBinHelper.BatchProcess(allFiles);
       
            //-----------





            if (settingsService.CurrentSettings.LoadCaPacksByDefault)
           {
               var gamePath = settingsService.GetGamePathForCurrentGame();
               if (gamePath != null)
               {
                   var gameName = GameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName;
                   var loadRes = _packfileService.LoadAllCaFiles(gamePath, gameName);
                   if (!loadRes)
                       MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
               }
           }

            if (settingsService.CurrentSettings.IsDeveloperRun)
            {
                //CampaignAnimationCreator_Debug.CreateDamselEditor(this, toolFactory, packfileService);
              // MountAnimationCreator_Debug.CreateRaptorAndHu01d(this, toolFactory, packfileService);
                KitbashEditor_Debug.CreateSlayerHead(this, toolFactory, packfileService);
                //AnimationPackEditor_Debug.Load(this, toolFactory, packfileService);

                //CreateEmptyEditor(editorView);
                CreateTestPackFiles(packfileService);
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
            var caPack = packfileService.Database.PackFiles[0];
            var newPackFile = packfileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            //packfileService.CopyFileFromOtherPackFile(caPack, @"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2", newPackFile);

            //var loadedPackFile = packfileService.Load(@"C:\Users\ole_k\Desktop\TestPackfile\SlayerMod.pack");

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

            var editorViewModel = ToolsFactory.GetToolViewModelFromFileName(file.Name);
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
