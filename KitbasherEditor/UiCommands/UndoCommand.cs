using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Components.Component;
using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class UndoCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Undo Last item";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Custom;
        public Hotkey HotKey { get; } = new Hotkey(Key.Z, ModifierKeys.Control);

        private readonly CommandExecutor _commandExecutor;

        public UndoCommand(CommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        public void Execute() => _commandExecutor.Undo();
    }
}
