using CommonControls.Events.UiCommands;
using SharedCore.ToolCreation;

namespace AssetEditor.UiCommands
{
    public class OpenEditorCommand : IUiCommand
    {
        private readonly IToolFactory _toolFactory;
        private readonly IEditorCreator _editorCreator;

        public OpenEditorCommand(IToolFactory toolFactory, IEditorCreator editorCreator)
        {
            _toolFactory = toolFactory;
            _editorCreator = editorCreator;
        }

        public void Execute<T>() where T : IEditorViewModel
        {
            var editorView = _toolFactory.Create<T>();
            _editorCreator.CreateEmptyEditor(editorView);
        }
    }
}
