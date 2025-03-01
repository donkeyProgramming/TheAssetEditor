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
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
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

        [ObservableProperty] public partial IEditorManager EditorManager { get; set; }
        [ObservableProperty] public partial bool IsClosingWithoutPrompt { get; set; }
        [ObservableProperty] public partial string ApplicationTitle { get; set; }
        [ObservableProperty] public partial string CurrentGame { get; set; }
        [ObservableProperty] public partial string EditablePackFile { get; set; }
        [ObservableProperty] public partial bool IsPackFileExplorerVisible { get; set; } = true;
        [ObservableProperty] public partial GridLength FileTreeColumnWidth { get; set; } = new GridLength(0.28, GridUnitType.Star);


        public MainViewModel(
                IEditorManager editorManager,
                PackFileTreeViewFactory packFileBrowserBuilder,
                MenuBarViewModel menuViewModel, 
                IPackFileService packfileService, 
                IEditorDatabase toolFactory, 
                IUiCommandFactory uiCommandFactory, 
                IEventHub eventHub,
                ApplicationSettingsService applicationSettingsService)
        {
            MenuBar = menuViewModel;

            EditorManager = editorManager;
            _uiCommandFactory = uiCommandFactory;

            eventHub.Register<PackFileContainerSetAsMainEditableEvent>(this, SetStatusBarEditablePackFile);

            FileTree = packFileBrowserBuilder.Create(ContextMenuType.MainApplication, showCaFiles: true, showFoldersOnly: false);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            ApplicationTitle = $"AssetEditor v{VersionChecker.CurrentVersion}";
            CurrentGame = $"Current Game: {GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame).DisplayName}";
        }

        void OpenFile(PackFile file) => _uiCommandFactory.Create<OpenEditorCommand>().Execute(file);

        [RelayCommand] private void Closing(IEditorInterface editor) 
        {
            var hasUnsavedPackFiles = FileTree.Files.Any(node => node.UnsavedChanged);
            if (EditorManager.ShouldBlockCloseCommand(editor, hasUnsavedPackFiles))
            {
                IsClosingWithoutPrompt = true;
                return;
            }

            IsClosingWithoutPrompt = MessageBox.Show(
                "You have unsaved changes. Do you want to quit without saving?",
                "Quit Without Saving",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        [RelayCommand] void CloseTool(IEditorInterface tool) => EditorManager.CloseTool(tool);
      
        [RelayCommand] void CloseOtherTools(IEditorInterface tool) => EditorManager.CloseOtherTools(tool);
        [RelayCommand] void CloseAllTools(IEditorInterface tool) => EditorManager.CloseAllTools(tool);
        [RelayCommand] void CloseToolsToLeft(IEditorInterface tool) => EditorManager.CloseToolsToLeft(tool);
        [RelayCommand] void CloseToolsToRight(IEditorInterface tool) => EditorManager.CloseToolsToRight(tool);

        public bool AllowDrop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default) => true;
        public bool Drop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default) => EditorManager.Drop(node, targetNode, insertAfterTargetNode);

        private void SetStatusBarEditablePackFile(PackFileContainerSetAsMainEditableEvent e)
        {
            EditablePackFile = e.Container != null ? $"Editable Pack: {e.Container.Name}" : "Editable Pack: None Set";
        }
    }
}
