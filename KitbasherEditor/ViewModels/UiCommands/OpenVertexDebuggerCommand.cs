using CommonControls.Common;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views.EditorViews.VertexDebugger;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenVertexDebuggerCommand : IExecutableUiCommand
    {

        private readonly SubToolWindowCreator _subToolWindowCreator;

        public OpenVertexDebuggerCommand(SubToolWindowCreator subToolWindowCreator)
        {
            _subToolWindowCreator = subToolWindowCreator;
        }

        public void Execute()
        {
            _subToolWindowCreator.CreateComponentWindow<VertexDebuggerView, VertexDebuggerViewModel>("Vertex debugger", 1200, 1100);
        }
    }
}
