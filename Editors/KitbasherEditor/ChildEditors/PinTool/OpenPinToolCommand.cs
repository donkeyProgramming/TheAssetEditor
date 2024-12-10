using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.ViewModels.PinTool;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public class OpenPinToolCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the pin tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        private readonly IAbstractFormFactory<PinToolWindow> _windowFactory;

        public OpenPinToolCommand(IAbstractFormFactory<PinToolWindow> windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create();
            window.Topmost = true;
            window.Show();
        }
    }
}
