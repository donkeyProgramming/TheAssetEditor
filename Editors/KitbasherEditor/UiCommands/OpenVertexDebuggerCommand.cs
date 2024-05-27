using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views.EditorViews.VertexDebugger;
using Shared.Ui.BaseDialogs;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenVertexDebuggerCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open vertex debugger";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrVertexSelected;
        public Hotkey HotKey { get; } = null;

        private readonly IWindowFactory _windowFactory;

        public OpenVertexDebuggerCommand(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create<VertexDebuggerViewModel, VertexDebuggerView>("Vertex debugger", 1200, 1100);
            window.ShowWindow();
        }
    }
}
