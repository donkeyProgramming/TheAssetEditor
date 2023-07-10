using AssetEditor.UiCommands;
using AssetEditor.Views.Settings;
using CommonControls.Common;
using CommonControls.Events.UiCommands;
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
    public class MainViewModel : NotifyPropertyChangedImpl, IDropTarget<IEditorViewModel, bool>
    {
        private readonly PackFileService _packfileService;
        private readonly DevelopmentConfiguration _developmentConfiguration;
        private readonly IUiCommandFactory _uiCommandFactory;

        public PackFileBrowserViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public IToolFactory ToolsFactory { get; set; }
        public ObservableCollection<IEditorViewModel> CurrentEditorsList { get; set; } = new ObservableCollection<IEditorViewModel>();

        int _selectedEditorIndex;
        public int SelectedEditorIndex { get => _selectedEditorIndex; set => SetAndNotify(ref _selectedEditorIndex, value); }

        public ICommand CloseToolCommand { get; set; }
        public ICommand CloseOtherToolsCommand { get; set; }
        public ICommand ClosingCommand { get; set; }

        private bool _isClosingWithoutPrompt;
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

        public MainViewModel(GameInformationFactory gameInformationFactory,
            MenuBarViewModel menuViewModel,
            IServiceProvider serviceProvider,
            PackFileService packfileService,
            ApplicationSettingsService settingsService,
            IToolFactory toolFactory,
            DevelopmentConfiguration developmentConfiguration,
            IUiCommandFactory uiCommandFactory)
        {
            MenuBar = menuViewModel;
            _developmentConfiguration = developmentConfiguration;
            _uiCommandFactory = uiCommandFactory;
            _packfileService = packfileService;
            _packfileService.Database.BeforePackFileContainerRemoved += Database_BeforePackFileContainerRemoved;

            CloseToolCommand = new RelayCommand<IEditorViewModel>(CloseTool);
            CloseOtherToolsCommand = new RelayCommand<IEditorViewModel>(CloseOtherTools);
            ClosingCommand = new RelayCommand<IEditorViewModel>(Closing);
            CloseAllToolsCommand = new RelayCommand<IEditorViewModel>(CloseAllTools);
            CloseToolsToRightCommand = new RelayCommand<IEditorViewModel>(CloseToolsToRight);
            CloseToolsToLeftCommand = new RelayCommand<IEditorViewModel>(CloseToolsToLeft);

            FileTree = new PackFileBrowserViewModel(_packfileService);
            FileTree.ContextMenu = new DefaultContextMenuHandler(_packfileService, toolFactory);
            FileTree.FileOpen += OpenFile;

            // eventHub<FileOpened>
            // eventHub<Database_BeforePackFileContainerRemoved>

            ToolsFactory = toolFactory;

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
                    var gameName = gameInformationFactory.GetGameById(settingsService.CurrentSettings.CurrentGame).DisplayName;
                    var loadRes = _packfileService.LoadAllCaFiles(gamePath, gameName);
                    if (!loadRes)
                        MessageBox.Show($"Unable to load all CA packfiles in {gamePath}");
                }
            }
        }

        void OpenFile(PackFile file) => _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);

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

        private bool Database_BeforePackFileContainerRemoved(PackFileContainer container)
        {
            var openFiles = CurrentEditorsList
                .Where(x => x.MainFile != null && _packfileService.GetPackFileContainer(x.MainFile) == container)
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

        void CloseTool(IEditorViewModel tool)
        {
            if (tool.HasUnsavedChanges)
            {
                if (MessageBox.Show("Unsaved changed - Are you sure?", "Close", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var index = CurrentEditorsList.IndexOf(tool);
            CurrentEditorsList.RemoveAt(index);
            ToolsFactory.DestroyEditor(tool);
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
            foreach (var editorViewModel in CurrentEditorsList)
                CloseTool(editorViewModel);

        }

        void CloseToolsToLeft(IEditorViewModel tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (int i = index - 1; i >= 0; i--)
            {
                CloseTool(CurrentEditorsList[0]);
            }
        }

        void CloseToolsToRight(IEditorViewModel tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (int i = CurrentEditorsList.Count - 1; i > index; i--)
            {
                CloseTool(CurrentEditorsList[i]);
            }
        }



        public bool AllowDrop(IEditorViewModel node, IEditorViewModel targeNode = default, bool insertAfterTargetNode = default) => true;


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

    public class EditorCreator : IEditorCreator
    {
        private readonly ILogger _logger = Logging.Create<EditorCreator>();
        private readonly MainViewModel _mainViewModel;
        private readonly PackFileService _packFileService;
        private readonly IToolFactory _toolFactory;

        public EditorCreator(MainViewModel mainEditorWindow, PackFileService packFileService, IToolFactory toolFactory)
        {
            _mainViewModel = mainEditorWindow;
            _packFileService = packFileService;
            _toolFactory = toolFactory;
        }

        public void CreateEmptyEditor(IEditorViewModel editorView)
        {
            _mainViewModel.CurrentEditorsList.Add(editorView);
            _mainViewModel.SelectedEditorIndex = _mainViewModel.CurrentEditorsList.Count - 1;
        }

        public void OpenFile(PackFile file)
        {
            if (file == null)
            {
                _logger.Here().Error($"Attempting to open file, but file is NULL");
                return;
            }

            var fileAlreadyAdded = _mainViewModel.CurrentEditorsList.FirstOrDefault(x => x.MainFile == file);
            if (fileAlreadyAdded != null)
            {
                _mainViewModel.SelectedEditorIndex = _mainViewModel.CurrentEditorsList.IndexOf(fileAlreadyAdded);
                _logger.Here().Information($"Attempting to open file '{file.Name}', but is is already open");
                return;
            }

            var fullFileName = _packFileService.GetFullPath(file);
            var editorViewModel = _toolFactory.Create(fullFileName);

            _logger.Here().Information($"Opening {file.Name} with {editorViewModel.GetType().Name}");
            editorViewModel.MainFile = file;
            _mainViewModel.CurrentEditorsList.Add(editorViewModel);
            _mainViewModel.SelectedEditorIndex = _mainViewModel.CurrentEditorsList.Count - 1;
        }
    }

}
