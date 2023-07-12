using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views.EditorViews.VertexDebugger;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenVertexDebuggerCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open vertex debugger";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrVertexSelected;
        public Hotkey HotKey { get; } = null;

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
