using System;
using System.Windows;
using System.Windows.Controls;
using AssetEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace AssetEditor.Services
{
    public class EditorCreator : IEditorCreator
    {
        private readonly ILogger _logger = Logging.Create<EditorCreator>();
        private readonly MainViewModel _mainViewModel;
        private readonly PackFileService _packFileService;
        private readonly IEditorDatabase _toolFactory;

        public EditorCreator(MainViewModel mainEditorWindow, PackFileService packFileService, IEditorDatabase toolFactory)
        {
            _mainViewModel = mainEditorWindow;
            _packFileService = packFileService;
            _toolFactory = toolFactory;
        }

        public IEditorInterface CreateFromFile(PackFile file, EditorEnums? preferedEditor)
        {
            if (file == null)
            {
                _logger.Here().Error($"Attempting to open file, but file is NULL");
                return null;
            }

            var fullFileName = _packFileService.GetFullPath(file);
            var editorViewModel = _toolFactory.Create(fullFileName, preferedEditor);
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
                for (var i = 0; i < _mainViewModel.CurrentEditorsList.Count; i++)
                {
                    var existingEditor = _mainViewModel.CurrentEditorsList[i];
                    if (existingEditor is IFileEditor existingFileEditor)
                    {
                        if (existingFileEditor.CurrentFile == file)
                        {
                            _logger.Here().Information($"Attempting to open file '{file.Name}', but is is already open");
                            _mainViewModel.SelectedEditorIndex = i;
                            return _mainViewModel.CurrentEditorsList[i];
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

        public IEditorInterface Create(EditorEnums editor,  Action<IEditorInterface>? onInitializeCallback = null)
        {
            var editorViewModel = _toolFactory.Create(editor);
            if (onInitializeCallback != null)
                onInitializeCallback(editorViewModel);
            
            InsertEditorIntoTab(editorViewModel);
            return editorViewModel;
        }

        public Window CreateWindow(PackFile packFile, EditorEnums? preferedEditor = null)
        {
            var fullFileName = _packFileService.GetFullPath(packFile);
            var editorViewModel = _toolFactory.Create(fullFileName, preferedEditor);

            if (editorViewModel is IFileEditor fileEditor)
                fileEditor.LoadFile(packFile);

            var toolView = _toolFactory.GetViewTypeFromViewModel(editorViewModel.GetType());
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
            _mainViewModel.CurrentEditorsList.Add(editorView);
            _mainViewModel.SelectedEditorIndex = _mainViewModel.CurrentEditorsList.Count - 1;
        }
    }
}
