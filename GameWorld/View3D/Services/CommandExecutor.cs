using GameWorld.Core.Commands;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using System;
using System.Collections.Generic;

namespace GameWorld.Core.Services
{
    public class CommandStackChangedEvent
    {
        public string HintText { get; internal set; }
        public bool IsMutation { get; internal set; }
    }

    public class CommandStackUndoEvent
    {
        public string HintText { get; set; }
    }

    public class CommandExecutor
    {
        protected ILogger _logger = Logging.Create<CommandExecutor>();
        private readonly Stack<ICommand> _commands = new Stack<ICommand>();
        private readonly IEventHub _eventHub;

        public CommandExecutor(IEventHub eventHub)
        {
            _eventHub = eventHub;
        }

        public void ExecuteCommand(ICommand command, bool isUndoable = true)
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
                _eventHub.Publish(new CommandStackChangedEvent()
                {
                    HintText = command.HintText,
                    IsMutation = command.IsMutation,
                });
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

                _eventHub.Publish(new CommandStackUndoEvent() { HintText = GetUndoHint() });
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

