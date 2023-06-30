﻿using CommonControls.Common;
using CommonControls.Events;
using MediatR;
using Serilog;
using System;
using System.Collections.Generic;
using View3D.Commands;
using View3D.Utility;

namespace View3D.Components.Component
{
    public delegate void CommandStackChangedDelegate();

    public class CommandStackChangedEvent : INotification
    {
        public string HintText { get; internal set; }
        public bool IsMutation { get; internal set; }
    }

    public class CommandStackUndoEvent : INotification
    {
        public string HintText { get; set; }
    }

    public class CommandExecutor
    {
        protected ILogger _logger = Logging.Create<CommandExecutor>();
        private readonly Stack<ICommand> _commands = new Stack<ICommand>();
        private readonly IMediator _mediator;
        private readonly ComponentManagerResolver _componentManagerResolver;

        public CommandExecutor(IMediator mediator, ComponentManagerResolver componentManagerResolver) 
        {
            _mediator = mediator;
            _componentManagerResolver = componentManagerResolver;
        }

        public void ExecuteCommand(ICommand command, bool isUndoable = true)
        {
            if (command == null)
                throw new ArgumentNullException("Command is null");
            if(isUndoable)
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
                _mediator.PublishSync(new CommandStackChangedEvent() 
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


                _mediator.PublishSync(new CommandStackUndoEvent() { HintText = GetUndoHint() });
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
