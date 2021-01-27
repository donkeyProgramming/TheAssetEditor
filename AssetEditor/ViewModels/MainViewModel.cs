using AssetEditor.Services;
using AssetEditor.Test;
using AssetEditor.ViewModels.FileTreeView;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Behaviors;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TextEditor;

namespace AssetEditor.ViewModels
{



    class TextEditor : IEditorViewModel
    {
        public string DisplayName { get; set; }
        public IPackFile MainFile { get; set; }

        public bool Save()
        {
            return true;
            //t
        }
    }

    class TextEditorB : IEditorViewModel
    {
        public string DisplayName { get; set; }
        public IPackFile MainFile { get; set; }

        public bool Save()
        {
            return true;
            //t
        }
    }

    class MainViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<MainViewModel>();

        public FileTreeViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public ToolFactory ToolFactory { get; set; }
        public ObservableCollection<IEditorViewModel> CurrentEditorsList { get; set; } = new ObservableCollection<IEditorViewModel>();
        
        int _selectedIndex;
        public int SelectedEditorIndex { get => _selectedIndex; set => SetAndNotify(ref _selectedIndex, value); }


        public MainViewModel(MenuBarViewModel menuViewModel, IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService, ToolFactory toolFactory)
        {
            ToolFactory = toolFactory;
            ToolFactory.RegisterToolAsDefault<TextEditorViewModel, TextEditorView>();


            //var res = ToolFactory.CreateToolAsWindow(new TextEditorViewModel());
            //res.ShowDialog();


            MenuBar = menuViewModel;

            //var n = typeof(BindableSelectedItemBehavior).Assembly.GetManifestResourceNames();
            // Properties.Resources.
            // var t = new BitmapImage(new Uri("pack://application:,,,/CommonControls;/Resources/Icons/icons8-3d-object-48.png", UriKind.Absolute));


            //C:\Program Files (x86)\Steam\steamapps\common\Total War WARHAMMER II\data\variants_dds2_sc.pack
            Startup();

            packfileService.LoadAllCaFiles(@"C:\Program Files (x86)\Steam\steamapps\common\Total War WARHAMMER II\data");
            FileTree = new FileTreeViewModel(packfileService);
            FileTree.FileOpen += OnFileOpen;

        }

        private void OnFileOpen(IPackFile file)
        {
            if (file == null)
            {
                _logger.Here().Error($"Attempting to open file, but file is NULL");
                return;
            }

            foreach (var item in CurrentEditorsList)
            {
                if (item.MainFile == file)
                {
                    _logger.Here().Information($"Attempting to open file '{file.Name}', but is is already open");
                    break;
                }
            }

            var editorViewModel = ToolFactory.GetToolViewModelFromFileName(file.Name);
            editorViewModel.MainFile = file;
            CurrentEditorsList.Add(editorViewModel);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }

        void Startup()
        {
          
        }
    }


    
}
