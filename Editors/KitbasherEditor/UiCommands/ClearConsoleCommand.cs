using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{

    public class ClearConsoleCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Clear the debug console window";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public ClearConsoleCommand() { }

        public void Execute() => Console.Clear();
    }
}
