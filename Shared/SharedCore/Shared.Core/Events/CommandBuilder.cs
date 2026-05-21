namespace Shared.Core.Events
{
    public class CommandBuilder<T> where T : IAeUndoCommandCommand
    {
        private readonly CommandManager _commandExecutor;
        private readonly T _command;
        private bool _isUndoable = true;

        public CommandBuilder(CommandManager commandExecutor, T command)
        {
            _commandExecutor = commandExecutor;
            _command = command;
        }

        public T Build() => _command;

        public void BuildAndExecute() => _commandExecutor.ExecuteCommand(_command, _isUndoable);

        public CommandBuilder<T> Configure(Action<T> predicate)
        {
            predicate(_command);
            return this;
        }

        public CommandBuilder<T> IsUndoable(bool isUndoable)
        {
            _isUndoable = isUndoable;
            return this;
        }
    }
}
