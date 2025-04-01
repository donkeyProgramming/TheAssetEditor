using System.Diagnostics;
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

        public IEditorInterface Execute(PackFile file, EditorEnums? preferedEditor = null)
        {
            var editor = _editorCreator.CreateFromFile(file, preferedEditor);
            return editor;
        }

        public IEditorInterface Execute(string file, EditorEnums? preferedEditor = null)
        {
            var packFile = _packFileService.FindFile(file);
            Debug.Assert(packFile != null, "File not found: " + file);
            
            return Execute(packFile, preferedEditor);
        }

        public IEditorInterface Execute(EditorEnums editorEnum)
        {
            var editor = _editorCreator.Create(editorEnum);
            return editor;
        }

        public void ExecuteAsWindow(string fileName, int width, int heigh)
        {
            var file = _packFileService.FindFile(fileName);
            var window = _editorCreator.CreateWindow(file);

            window.Width = width;
            window.Height = heigh;
            window.ShowDialog();
        }

        public void ExecuteAsWindow(EditorEnums preferedEditor,  int width, int heigh)
        {
            var window = _editorCreator.CreateWindow(null, preferedEditor);
            window.Width = width;
            window.Height = heigh;
            window.ShowDialog();
        }
    }
}
