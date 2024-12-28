using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace AssetEditor.Services
{
    public interface IEditorManager : IEditorCreator
    {
        IList<IEditorInterface> GetAllEditors();
        int GetCurrentEditor();

        public void CloseTool(IEditorInterface tool);
        public bool ShouldBlockCloseCommand(IEditorInterface editor, bool hasUnsavedFiles);

        public void CloseOtherTools(IEditorInterface tool);
        public void CloseAllTools(IEditorInterface tool);
        public void CloseToolsToLeft(IEditorInterface tool);
        public void CloseToolsToRight(IEditorInterface tool);
        public bool Drop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default);
    }

    public partial class EditorManager : ObservableObject, IEditorManager
    {
        private readonly ILogger _logger = Logging.Create<EditorManager>();

        private readonly IPackFileService _packFileService;
        private readonly IEditorDatabase _editorDatabase;

        public ObservableCollection<IEditorInterface> CurrentEditorsList { get; set; } = [];
        [ObservableProperty] private int _selectedEditorIndex = -1;

        public EditorManager(IGlobalEventHub eventHub, IPackFileService packFileService, IEditorDatabase editorDatabase)
        {
            _packFileService = packFileService;
            _editorDatabase = editorDatabase;

            eventHub.Register<BeforePackFileContainerRemovedEvent>(this, OnBeforeRemoved);
            eventHub.Register<ForceShutdownEvent>(this, OnForceShutdownEditor);
        }

        public IList<IEditorInterface> GetAllEditors() => CurrentEditorsList;
        public int GetCurrentEditor() => SelectedEditorIndex;
        public IEditorInterface CreateFromFile(PackFile file, EditorEnums? preferedEditor)
        {
            if (file == null)
            {
                _logger.Here().Error($"Attempting to open file, but file is NULL");
                return null;
            }

            var fullFileName = _packFileService.GetFullPath(file);
            var editorViewModel = _editorDatabase.Create(fullFileName, preferedEditor);
            if (editorViewModel == null)
            {
                _logger.Here().Information($"No editor selected");
                return null;
            }

            // Attempt to load the assigned file, if the editor is a fileEditor.
            // TODO: Ensure we can only get here if we have a fileEditor
            if (editorViewModel is IFileEditor fileEditor)
            {
                // Ensure file is not already open
                for (var i = 0; i < CurrentEditorsList.Count; i++)
                {
                    var existingEditor = CurrentEditorsList[i];
                    if (existingEditor is IFileEditor existingFileEditor)
                    {
                        if (existingFileEditor.CurrentFile == file)
                        {
                            _logger.Here().Information($"Attempting to open file '{file.Name}', but is is already open");
                            SelectedEditorIndex = i;
                            return CurrentEditorsList[i];
                        }
                    }
                }

                // Open the file
                _logger.Here().Information($"Opening {file.Name} with {editorViewModel?.GetType().Name}");
                fileEditor.LoadFile(file);
            }

            InsertEditorIntoTab(editorViewModel);
            return editorViewModel;
        }

        public IEditorInterface Create(EditorEnums editor, Action<IEditorInterface>? onInitializeCallback = null)
        {
            var editorViewModel = _editorDatabase.Create(editor);
            if (onInitializeCallback != null)
                onInitializeCallback(editorViewModel);

            InsertEditorIntoTab(editorViewModel);
            return editorViewModel;
        }

        public Window CreateWindow(PackFile packFile, EditorEnums? preferedEditor = null)
        {
            var fullFileName = _packFileService.GetFullPath(packFile);
            var editorViewModel = _editorDatabase.Create(fullFileName, preferedEditor);

            if (editorViewModel is IFileEditor fileEditor)
                fileEditor.LoadFile(packFile);

            var toolView = _editorDatabase.GetViewTypeFromViewModel(editorViewModel.GetType());
            var instance = Activator.CreateInstance(toolView) as Control;

            var newWindow = new Window
            {
                Style = (Style)Application.Current.Resources["CustomWindowStyle"],
                Content = instance,
                DataContext = editorViewModel,
                Title = editorViewModel.DisplayName
            };

            return newWindow;
        }

        void InsertEditorIntoTab(IEditorInterface editorView)
        {
            CurrentEditorsList.Add(editorView);
            SelectedEditorIndex = CurrentEditorsList.Count - 1;
        }

        private void OnBeforeRemoved(BeforePackFileContainerRemovedEvent e)
        {
            var container = e.Removed;
            var openFiles = new List<IEditorInterface>();
            for (var i = 0; i < CurrentEditorsList.Count; i++)
            {
                if (CurrentEditorsList[i] is not IFileEditor fileEditor)
                    continue;

                var containterForPack = _packFileService.GetPackFileContainer(fileEditor.CurrentFile);
                if (containterForPack == container)
                    openFiles.Add(CurrentEditorsList[i]);
            }

            if (openFiles.Any())
            {
                if (MessageBox.Show($"Closing pack file '{container.Name}' with open files ({openFiles.First().DisplayName}), are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.AllowClose = false;
                }
            }

            foreach (var editor in openFiles)
            {
                CurrentEditorsList.Remove(editor);
                editor.Close();
            }
        }

        private void OnForceShutdownEditor(ForceShutdownEvent e)
        {
            _logger.Here().Warning($"Attempting to force shutdown editor {e.EditorHandle.DisplayName}");
            CloseTool(e.EditorHandle);
        }

        public void CloseTool(IEditorInterface tool)
        {
            if (tool is ISaveableEditor saveableEditor && saveableEditor.HasUnsavedChanges)
            {
                if (MessageBox.Show("Unsaved changes - Are you sure?", "Close", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }

            var index = CurrentEditorsList.IndexOf(tool);
            CurrentEditorsList.RemoveAt(index);
            _editorDatabase.DestroyEditor(tool);
            tool.Close();
        }

        public  void CloseOtherTools(IEditorInterface tool)
        {
            foreach (var editorViewModel in CurrentEditorsList.ToList())
            {
                if (editorViewModel != tool)
                    CloseTool(editorViewModel);
            }
        }

        public void CloseAllTools(IEditorInterface tool)
        {
            foreach (var editorViewModel in CurrentEditorsList)
                CloseTool(editorViewModel);
        }

        public void CloseToolsToLeft(IEditorInterface tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (var i = index - 1; i >= 0; i--)
                CloseTool(CurrentEditorsList[i]);
        }

        public void CloseToolsToRight(IEditorInterface tool)
        {
            var index = CurrentEditorsList.IndexOf(tool);
            for (var i = CurrentEditorsList.Count - 1; i > index; i--)
                CloseTool(CurrentEditorsList[i]);
        }

        public bool ShouldBlockCloseCommand(IEditorInterface editor, bool hasUnsavedFiles)
        {
            var hasUnsavedEditorChanges = CurrentEditorsList.Where(x => x is ISaveableEditor).Cast<ISaveableEditor>().Any(x => x.HasUnsavedChanges);

            if (!(hasUnsavedFiles || hasUnsavedEditorChanges))
                return true;

            return false;
        }

        // Move to a drop handler 
        public bool Drop(IEditorInterface node, IEditorInterface targetNode = default, bool insertAfterTargetNode = default)
        {
            var nodeIndex = CurrentEditorsList.IndexOf(node);
            var targetNodeIndex = CurrentEditorsList.IndexOf(targetNode);

            // if tabs next to each other switch places
            if (Math.Abs(nodeIndex - targetNodeIndex) == 1) 
            {
                (CurrentEditorsList[nodeIndex], CurrentEditorsList[targetNodeIndex]) = (CurrentEditorsList[targetNodeIndex], CurrentEditorsList[nodeIndex]);
            }
            // if tabs are not next to each other decide based on insertAfterTargetNode
            else
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
