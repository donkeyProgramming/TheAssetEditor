using System.Windows;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.ChildEditors.VertexDebugger
{
    public class OpenVertexDebuggerCommand : IScopedKitbasherUiCommand, IDisposable
    {
        public string ToolTip { get; set; } = "Open vertex debugger";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrVertexSelected;
        public Hotkey HotKey { get; } = null;
        private readonly IAbstractFormFactory<VertexDebuggerWindow> _windowFactory;
        private Window? _windowInstance;

        public OpenVertexDebuggerCommand(IAbstractFormFactory<VertexDebuggerWindow> windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            if (_windowInstance == null)
            {
                _windowInstance = _windowFactory.Create();
                _windowInstance.Show();
                _windowInstance.Closed += OnWindowClosed;
            }
            else
            {
                _windowInstance.BringIntoView();
            }
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_windowInstance != null)
                _windowInstance.Closed-= OnWindowClosed;

           _windowInstance = null;
        }

        public void Dispose()
        {
            if (_windowInstance != null)
                _windowInstance.Close();
            _windowInstance = null;
        }
    }
}
