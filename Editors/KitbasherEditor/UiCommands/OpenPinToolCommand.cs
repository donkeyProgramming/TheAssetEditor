using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.PinTool;
using KitbasherEditor.Views.EditorViews.PinTool;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenPinToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the pin tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly IWindowFactory _windowFactory;

        public OpenPinToolCommand(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create<PinToolViewModel, PinToolView>("Pin tool", 360, 415);
            window.AlwaysOnTop = true;
            window.ShowWindow();
        }
    }
}
