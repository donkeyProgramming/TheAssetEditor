using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.ViewModels.PinTool;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.ChildEditors.PinTool.UiCommands
{
    public class OpenPinToolCommand : ITransientKitbasherUiCommand, IDisposable
    {
        public string ToolTip { get; set; } = "Open the pin tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        private readonly IAbstractFormFactory<PinToolWindow> _windowFactory;
        PinToolWindow? _windowInstance;

        public OpenPinToolCommand(IAbstractFormFactory<PinToolWindow> windowFactory)
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
                _windowInstance.Closed -= OnWindowClosed;

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
