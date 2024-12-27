using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;
using GameWorld.Core.Services;
using Editors.KitbasherEditor.Core.MenuBarViews;

namespace Editors.KitbasherEditor.UiCommands
{
    public class UndoCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Undo Last item";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Custom;
        public Hotkey? HotKey { get; } = new Hotkey(Key.Z, ModifierKeys.Control);

        private readonly CommandExecutor _commandExecutor;

        public UndoCommand(CommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        public void Execute() => _commandExecutor.Undo();
    }
}
