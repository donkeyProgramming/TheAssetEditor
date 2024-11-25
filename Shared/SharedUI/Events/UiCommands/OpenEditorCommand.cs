using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Shared.Ui.Events.UiCommands
{
    public class OpenEditorCommand : IUiCommand
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileService _packFileService;

        public OpenEditorCommand(IEditorCreator editorCreator, IPackFileService packFileService)
        {
            _packFileService = packFileService;
            _editorCreator = editorCreator;
        }

        public void Execute(PackFile file, EditorEnums? preferedEditor = null)
        {
            _editorCreator.CreateFromFile(file, preferedEditor);
        }

        public void Execute(EditorEnums editorEnum)
        {
            _editorCreator.Create(editorEnum);
        }

        public void ExecuteAsWindow(string fileName, int width, int heigh)
        {
            var file = _packFileService.FindFile(fileName);
            var window = _editorCreator.CreateWindow(file);

            window.Width = width;
            window.Height = heigh;
            window.ShowDialog();
        }

    }
}
