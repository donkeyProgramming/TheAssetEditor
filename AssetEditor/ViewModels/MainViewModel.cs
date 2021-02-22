using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Behaviors;
using CommonControls.PackFileBrowser;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using KitbasherEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TextEditor;
using View3D;
using View3D.Scene;

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

        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService, ToolFactory toolFactory)
        {
            MenuBar = menuViewModel;

            FileTree = new FileTreeViewModel(packfileService);
            FileTree.FileOpen += OnFileOpen;

            ToolsFactory = toolFactory;
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
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2");
            //var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\bc4\hef\hef_war_lion\hef_war_lion_02.rigid_model_v2");
            OnFileOpen(packFile);
            // var window = _toolFactory.CreateToolAsWindow<KitbasherViewModel>(out var editorViewModel);
            // editorViewModel.MainFile = packFile;
            // window.Width = 800;
            // window.Height = 600;
            // window.ShowDialog();
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
    }
}
