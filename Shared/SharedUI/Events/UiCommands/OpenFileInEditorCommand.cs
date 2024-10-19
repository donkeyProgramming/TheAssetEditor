using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Shared.Ui.Events.UiCommands
{
    public class OpenFileInEditorCommand : IUiCommand
    {
        private readonly IEditorCreator _editorCreator;

        public OpenFileInEditorCommand(IEditorCreator editorCreator)
        {
            _editorCreator = editorCreator;
        }

        public void Execute(PackFile file, EditorEnums? preferedEditor = null)
        {
            _editorCreator.OpenFile(file, preferedEditor);
        }
    }
}
