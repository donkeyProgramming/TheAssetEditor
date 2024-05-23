using Shared.Core.Events;
using SharedCore.PackFiles.Models;
using SharedCore.ToolCreation;

namespace AssetEditor.UiCommands
{
    public class OpenFileInEditorCommand : IUiCommand
    {
        private readonly IEditorCreator _editorCreator;

        public OpenFileInEditorCommand(IEditorCreator editorCreator)
        {
            _editorCreator = editorCreator;
        }

        public void Execute(PackFile file)
        {
            _editorCreator.OpenFile(file);
        }
    }
}
