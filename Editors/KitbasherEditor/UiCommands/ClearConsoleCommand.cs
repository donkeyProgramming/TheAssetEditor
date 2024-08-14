using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{

    public class ClearConsoleCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Clear the debug console window";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public ClearConsoleCommand() { }

        public void Execute() => Console.Clear();
    }
}
