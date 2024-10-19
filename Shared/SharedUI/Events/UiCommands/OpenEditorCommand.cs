using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Shared.Ui.Events.UiCommands
{
    public class OpenEditorCommand : IUiCommand
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public OpenEditorCommand(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _toolFactory = toolFactory;
            _packFileService = packFileService;
            _editorCreator = editorCreator;
        }

        public void Execute(PackFile file, EditorEnums? preferedEditor = null)
        {
            _editorCreator.CreateFromFile(file, preferedEditor);
        }

        public void Execute<T>() where T : IEditorViewModel
        {
            var editorView = _toolFactory.Create<T>();
            _editorCreator.Create(editorView);
        }

        public void ExecuteAsWindow(string fileName, int width, int heigh)
        {
            
            var viewModel = _toolFactory.Create(fileName);
            if (viewModel is IFileEditor fileEditor)
                fileEditor.LoadFile(_packFileService.FindFile(fileName));

            var window = _toolFactory.CreateAsWindow(viewModel);
            window.Width = width;
            window.Height = heigh;
            window.Title = viewModel.DisplayName;
            window.ShowDialog();
        }
    }
}
