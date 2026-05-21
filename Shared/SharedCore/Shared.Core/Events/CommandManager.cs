using Shared.Core.ErrorHandling;

namespace Shared.Core.Events
{
    public class CommandManager
    {
        protected ILogger _logger = Logging.Create<CommandManager>();
        private readonly Stack<IAeUndoCommandCommand> _commands = new Stack<IAeUndoCommandCommand>();
        private readonly IEventHub _eventHub;

        public CommandManager(IEventHub eventHub)
        {
            _eventHub = eventHub;
        }

        public void ExecuteCommand(IAeUndoCommandCommand command, bool isUndoable = true)
        {
            if (command == null)
                throw new ArgumentNullException("Command is null");
            if (isUndoable)
                _commands.Push(command);

            _logger.Here().Information($"Executing {command.GetType().Name}");
            try
            {
                command.Execute();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to execute command : {e}");
            }

            if (isUndoable)
            {
                _eventHub.Publish(new CommandStackChangedEvent(command.HintText, command.IsMutation));
            }
        }

        public bool CanUndo() => _commands.Count != 0;

        public void Undo()
        {
            if (CanUndo())
            {
                var command = _commands.Pop();
                _logger.Here().Information($"Undoing {command.GetType().Name}");
                try
                {
                    command.Undo();
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Failed to Undoing command : {e}");
                }

                var undoText = GetUndoHint();
                _eventHub.Publish(new CommandStackUndoEvent(undoText));
            }
        }

        public string GetUndoHint()
        {
            if (!CanUndo())
                return "No items to undo";

            return _commands.Peek().HintText;
        }
    }
}
