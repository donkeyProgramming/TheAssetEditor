using KitbasherEditor.ViewModels.MenuBarViews;
using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;
using GameWorld.Core.Services;

namespace Editors.KitbasherEditor.UiCommands
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
