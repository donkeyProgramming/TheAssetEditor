using System.Linq;
using System.Windows;
using AssetEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDropTarget<IEditorInterface, bool>
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public PackFileBrowserViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }
        public IEditorDatabase ToolsFactory { get; set; }

        [ObservableProperty] IEditorManager _editorManager;
        [ObservableProperty] private bool _isClosingWithoutPrompt;
        [ObservableProperty] private string _applicationTitle;
        [ObservableProperty] private string _currentGame;
        [ObservableProperty] private string _editablePackFile;

        public MainViewModel(
                IEditorManager editorManager,
                PackFileTreeViewBuilder packFileBrowserBuilder,
                MenuBarViewModel menuViewModel, 
                IPackFileService packfileService, 
                IEditorDatabase toolFactory, 
                IUiCommandFactory uiCommandFactory, 
                IEventHub eventHub,
                ApplicationSettingsService applicationSettingsService, 
                GameInformationFactory gameInformationFactory)
        {
            MenuBar = menuViewModel;

            _editorManager = editorManager;
            _uiCommandFactory = uiCommandFactory;

            eventHub.Register<PackFileContainerSetAsMainEditableEvent>(this, SetStatusBarEditablePackFile);

            FileTree = packFileBrowserBuilder.Create(ContextMenuType.MainApplication, true);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            ApplicationTitle = $"AssetEditor v{VersionChecker.CurrentVersion}";
            CurrentGame = $"Current Game: {gameInformationFactory.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame).DisplayName}";
        }

        void OpenFile(PackFile file) => _uiCommandFactory.Create<OpenEditorCommand>().Execute(file);

        [RelayCommand] private void Closing(IEditorInterface editor) 
        {
            var hasUnsavedPackFiles = FileTree.Files.Any(node => node.UnsavedChanged);
            if (_editorManager.ShouldBlockCloseCommand(editor, hasUnsavedPackFiles))
            {
                IsClosingWithoutPrompt = true;
                return;
            }

            IsClosingWithoutPrompt = MessageBox.Show(
                "You have unsaved changes. Do you want to quit without saving?",
                "Quit Without Saving",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        [RelayCommand] void CloseTool(IEditorInterface tool) => _editorManager.CloseTool(tool);
        [RelayCommand] void CloseOtherTools(IEditorInterface tool) => _editorManager.CloseOtherTools(tool);
        [RelayCommand] void CloseAllTools(IEditorInterface tool) => _editorManager.CloseAllTools(tool);
        [RelayCommand] void CloseToolsToLeft(IEditorInterface tool) => _editorManager.CloseToolsToLeft(tool);
        [RelayCommand] void CloseToolsToRight(IEditorInterface tool) => _editorManager.CloseToolsToRight(tool);

        public bool AllowDrop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default) => true;
        public bool Drop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default) => _editorManager.Drop(node, targetNode, insertAfterTargetNode);

        private void SetStatusBarEditablePackFile(PackFileContainerSetAsMainEditableEvent e)
        {
            EditablePackFile = e.Container != null ? $"Editable Pack: {e.Container.Name}" : "Editable Pack: None Set";
        }
    }
}
