using GameWorld.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GameWorld.Core.Commands
{
    public interface ICommand
    {
        void Undo();
        void Execute();
        string HintText { get; }
        bool IsMutation { get; }
    }

    public class CommandBuilder<T> where T : ICommand
    {
        private readonly CommandExecutor _commandExecutor;
        private readonly T _command;
        private bool _isUndoable = true;

        public CommandBuilder(CommandExecutor commandExecutor, T command)
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

    public class CommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandExecutor _commandExecutor;

        public CommandFactory(IServiceProvider serviceProvider, CommandExecutor commandExecutor)
        {
            _serviceProvider = serviceProvider;
            _commandExecutor = commandExecutor;
        }

        public CommandBuilder<T> Create<T>() where T : ICommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            return new CommandBuilder<T>(_commandExecutor, instance); ;
        }
    }
}

