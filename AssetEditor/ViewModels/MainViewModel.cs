using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AssetEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDropTarget<EditorInterfaces, bool>
    {
        private readonly PackFileService _packfileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public PackFileBrowserViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }
        public IEditorDatabase ToolsFactory { get; set; }
        public ObservableCollection<EditorInterfaces> CurrentEditorsList { get; set; } = new ObservableCollection<EditorInterfaces>();

        [ObservableProperty] private int _selectedEditorIndex;
        [ObservableProperty] private bool _isClosingWithoutPrompt;
        [ObservableProperty] private string _applicationTitle;
        [ObservableProperty] private string _currentGame;
        [ObservableProperty] private string _editablePackFile;

        public MainViewModel(MenuBarViewModel menuViewModel, PackFileService packfileService, IEditorDatabase toolFactory, IUiCommandFactory uiCommandFactory, IExportFileContextMenuHelper exportFileContextMenuHelper, ApplicationSettingsService applicationSettingsService, GameInformationFactory gameInformationFactory)
        {
            MenuBar = menuViewModel;
            _uiCommandFactory = uiCommandFactory;
            _packfileService = packfileService;
            _packfileService.Database.BeforePackFileContainerRemoved += Database_BeforePackFileContainerRemoved;
            _packfileService.Database.ContainerUpdated += OnContainerUpdated;

            FileTree = new PackFileBrowserViewModel(_packfileService);
            FileTree.ContextMenu = new DefaultContextMenuHandler(_packfileService, uiCommandFactory, exportFileContextMenuHelper);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            ApplicationTitle = $"AssetEditor v{VersionChecker.CurrentVersion}";
            CurrentGame = $"Current Game: {gameInformationFactory.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame).DisplayName}";
            SetStatusBarEditablePackFile();
        }

        void OpenFile(PackFile file) => _uiCommandFactory.Create<OpenEditorCommand>().Execute(file);

        [RelayCommand] private void Closing(EditorInterfaces editor)
        {
            var hasUnsavedEditorChanges = CurrentEditorsList.Where(x => x is ISaveableEditor).Cast<ISaveableEditor>().Any(x => x.HasUnsavedChanges);
            var hasUnsavedPackFiles = FileTree.Files.Any(node => node.UnsavedChanged);
            if (!(hasUnsavedPackFiles || hasUnsavedEditorChanges))
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

            var openFiles = new List<EditorInterfaces>();
            for (var i = 0; i < CurrentEditorsList.Count; i++)
            {
                if (CurrentEditorsList[i] is not IFileEditor fileEditor)
                    continue;

                var containterForPack = _packfileService.GetPackFileContainer(fileEditor.CurrentFile);
                if (containterForPack == container)
                    openFiles.Add(CurrentEditorsList[i]);
            }

            if (openFiles.Any())
            {
                if (MessageBox.Show($"Closing pack file '{container.Name}' with open files ({openFiles.First().DisplayName}), are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            foreach (var editor in openFiles)
            {
                CurrentEditorsList.Remove(editor);
                editor.Close();
            }

            return true;
        }

        [RelayCommand] void CloseTool(EditorInterfaces tool)
        {
            if (tool is ISaveableEditor saveableEditor && saveableEditor.HasUnsavedChanges)
            {
                if (MessageBox.Show("Unsaved changes - Are you sure?", "Close", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var index = CurrentEditorsList.IndexOf(tool);
            CurrentEditorsList.RemoveAt(index);
            ToolsFactory.DestroyEditor(tool);
            tool.Close();
        }

        [RelayCommand] void CloseOtherTools(EditorInterfaces tool)
        {
            foreach (var editorViewModel in CurrentEditorsList.ToList())
            {
                if (editorViewModel != tool)
                    CloseTool(editorViewModel);
            }
        }

        [RelayCommand] void CloseAllTools(EditorInterfaces tool)
        {
            foreach (var editorViewModel in CurrentEditorsList)
                CloseTool(editorViewModel);
        }

        [RelayCommand] void CloseToolsToLeft(EditorInterfaces tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (var i = index - 1; i >= 0; i--)
                CloseTool(CurrentEditorsList[i]);
        }

        [RelayCommand] void CloseToolsToRight(EditorInterfaces tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (var i = CurrentEditorsList.Count - 1; i > index; i--)
                CloseTool(CurrentEditorsList[i]);
        }

        public bool AllowDrop(EditorInterfaces node, EditorInterfaces targetNode = default, bool insertAfterTargetNode = default) => true;

        public bool Drop(EditorInterfaces node, EditorInterfaces targetNode = default, bool insertAfterTargetNode = default)
        {
            var nodeIndex = CurrentEditorsList.IndexOf(node);
            var targetNodeIndex = CurrentEditorsList.IndexOf(targetNode);

            if (Math.Abs(nodeIndex - targetNodeIndex) == 1) // if tabs next to each other switch places
                (CurrentEditorsList[nodeIndex], CurrentEditorsList[targetNodeIndex]) = (CurrentEditorsList[targetNodeIndex], CurrentEditorsList[nodeIndex]);
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

        private void OnContainerUpdated(PackFileContainer container)
        {
            SetStatusBarEditablePackFile();
        }

        private void SetStatusBarEditablePackFile()
        {
            EditablePackFile = _packfileService.Database.PackSelectedForEdit != null ? $"Editable Pack: {_packfileService.Database.PackSelectedForEdit.Name}" : "Editable Pack: None Set";
        }
    }
}
