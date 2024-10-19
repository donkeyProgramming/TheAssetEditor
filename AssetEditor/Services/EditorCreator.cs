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

            var fullFileName = _packFileService.GetFullPath(file);
            var editorViewModel = _toolFactory.Create(fullFileName);

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
                            return;
                        }
                    }
                }

                // Open the file
                _logger.Here().Information($"Opening {file.Name} with {editorViewModel?.GetType().Name}");
                fileEditor.LoadFile(file);
            }

            _mainViewModel.CurrentEditorsList.Add(editorViewModel);
            _mainViewModel.SelectedEditorIndex = _mainViewModel.CurrentEditorsList.Count - 1;
        }
    }
}
