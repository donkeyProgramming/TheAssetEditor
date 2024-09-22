﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor.ViewModels
{
    public class MainViewModel : NotifyPropertyChangedImpl, IDropTarget<IEditorViewModel, bool>
    {
        private readonly PackFileService _packfileService;
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

        public string ApplicationTitle { get; set; }

        public MainViewModel( MenuBarViewModel menuViewModel,
            PackFileService packfileService,
            IToolFactory toolFactory,
            IUiCommandFactory uiCommandFactory,
            IExportFileContextMenuHelper exportFileContextMenuHelper,
            ApplicationSettingsService applicationSettingsService)
        {
            MenuBar = menuViewModel;
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
            FileTree.ContextMenu = new DefaultContextMenuHandler(_packfileService, toolFactory, uiCommandFactory, exportFileContextMenuHelper);
            FileTree.FileOpen += OpenFile;

            ToolsFactory = toolFactory;

            ApplicationTitle = $"AssetEditor v{VersionChecker.CurrentVersion} - {applicationSettingsService.CurrentSettings.CurrentGame}";
        }

        void OpenFile(PackFile file) => _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);

        private void Closing(IEditorViewModel editor)
        {
            var hasUnsavedEditorChanges = CurrentEditorsList
                .Where(x => x is ISaveableEditor)
                .Cast<ISaveableEditor>()
                .Any(x => x.HasUnsavedChanges);

            var hasUnsavedPackFiles = FileTree.Files.Any(node => node.UnsavedChanged);

            if ( !(hasUnsavedPackFiles || hasUnsavedEditorChanges) )
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
            if (tool is ISaveableEditor saveableEditor && saveableEditor.HasUnsavedChanges)
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
}
