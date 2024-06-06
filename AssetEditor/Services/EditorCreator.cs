using System.Linq;
using AssetEditor.ViewModels;
using KitbasherEditor.Views.EditorViews;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.WindowHandling;

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

        public void OpenFileInWindow(PackFile file, int width, int height)
        {
            var fullFileName = _packFileService.GetFullPath(file);
            var editorViewModel = _toolFactory.Create(fullFileName);
            
        }
    }
}
