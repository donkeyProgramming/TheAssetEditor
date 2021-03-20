using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace AssetEditor.ViewModels
{
    class MainViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<MainViewModel>();

        public FileTreeViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public ToolFactory ToolsFactory { get; set; }
        public ObservableCollection<IEditorViewModel> CurrentEditorsList { get; set; } = new ObservableCollection<IEditorViewModel>();

        int _selectedIndex;
        public int SelectedEditorIndex { get => _selectedIndex; set => SetAndNotify(ref _selectedIndex, value); }

        public ICommand CloseToolCommand { get; set; }

        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService, ToolFactory toolFactory)
        {
            MenuBar = menuViewModel;

            CloseToolCommand = new RelayCommand<IEditorViewModel>(CloseTool);

           
            FileTree = new FileTreeViewModel(packfileService);
            FileTree.FileOpen += OnFileOpen;

            ToolsFactory = toolFactory;
            //ToolsFactory.RegisterTool<TextEditorViewModel, TextEditorView>(".wsmodel", ".variantmeshdefinition");
            //ToolsFactory.RegisterToolAsDefault<TextEditorViewModel, TextEditorView>();

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
                var gamePath = settingsService.GetGamePathForCurrentGame();
                if (gamePath != null)
                {
                    var loadRes = packfileService.LoadAllCaFiles(gamePath);
                    if (!loadRes)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                }
            }


            //PackFileBrowserWindow b = new PackFileBrowserWindow(packfileService);
            //b.ShowDialog();
            //

            //
            //variantmeshes\variantmeshdefinitions\dwf_hammerers.variantmeshdefinition"
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_loremaster_of_hoeth\hef_loremaster_of_hoeth_head_01.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\bc4\hef\hef_war_lion\hef_war_lion_02.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hr1\brt\brt_royal_pegasus\brt_pegasus_01.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_props\hef_ranger_sword_1h_03.rigid_model_v2");
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\skvt1\skv\skv_jezzails\skv_clan_rats_legs_fr_01.rigid_model_v2"); 
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_eltharion\hef_eltharion_head.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu17\skv\skv_clan_rats\head\skv_clan_rats_head_04.rigid_model_v2");

            OnFileOpen(packFile);

            CreateTestPackFiles(packfileService);
        }

        void CreateTestPackFiles(PackFileService packfileService)
        {
            var caPack = packfileService.Database.PackFiles[0];
            var newPackFile = packfileService.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            packfileService.CopyFileFromOtherPackFile(caPack, @"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2", newPackFile);

            var loadedPackFile = packfileService.Load(@"C:\Users\ole_k\Desktop\TestPackfile\SlayerMod.pack");

            packfileService.SetEditablePack(loadedPackFile);
        }

        private void OnFileOpen(IPackFile file)
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
    }
}
