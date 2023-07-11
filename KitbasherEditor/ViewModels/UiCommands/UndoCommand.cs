using CommonControls.Events.UiCommands;
using View3D.Components.Component;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class UndoCommand : IExecutableUiCommand
    {
        private readonly CommandExecutor _commandExecutor;

        public UndoCommand(CommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        public void Execute() => _commandExecutor.Undo();
    }
}
