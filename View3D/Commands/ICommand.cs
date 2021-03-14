using Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;

namespace View3D.Commands
{
    public interface ICommand
    {
        void Undo();
        void Execute();
        void Initialize(IComponentManager componentManager);
    }


    public abstract class CommandBase<T> : ICommand
    {
        protected ILogger _logger = Logging.Create<T>();

        public void Undo()
        {
            _logger.Here().Information($"Undoing {typeof(T).Name}");
            try
            {
                UndoCommand();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to Undoing command : {e}");
            }
        }

        public virtual void Initialize(IComponentManager componentManager) { }

        public void Execute()
        {
            _logger.Here().Information($"Executing {typeof(T).Name}" );
            try
            {
                ExecuteCommand();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to execute command : {e}");
            }
        }


        protected abstract void ExecuteCommand();
        protected abstract void UndoCommand();
    }

    public class ComplexCommand : ICommand
    {
        List<ICommand> _subCommands = new List<ICommand>();

        public void Execute()
        {
            foreach (var command in _subCommands)
                command.Execute();
            throw new NotImplementedException();
        }

        public void Initialize(IComponentManager componentManager)
        {
            foreach (var command in _subCommands)
                command.Initialize(componentManager);
        }

        public void Undo()
        {
            _subCommands.Reverse();
            foreach (var command in _subCommands)
                command.Undo();
        }
    }
}

