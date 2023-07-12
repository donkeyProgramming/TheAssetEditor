using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using System;

namespace KitbasherEditor.ViewModels.UiCommands
{

    public class ClearConsoleCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Clear the debug console window";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public ClearConsoleCommand(){}

        public void Execute() => Console.Clear();
    }
}
