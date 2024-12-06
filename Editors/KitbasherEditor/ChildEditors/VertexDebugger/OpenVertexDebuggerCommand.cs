using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.ChildEditors.VertexDebugger
{
    public class OpenVertexDebuggerCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open vertex debugger";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrVertexSelected;
        public Hotkey HotKey { get; } = null;

        private readonly IAbstractFormFactory<VertexDebuggerWindow> _windowFactory;

        public OpenVertexDebuggerCommand(IAbstractFormFactory<VertexDebuggerWindow> windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create();
            window.Show();
        }
    }
}
