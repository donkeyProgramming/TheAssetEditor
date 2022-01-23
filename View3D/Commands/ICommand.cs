using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using View3D.Components.Component.Selection;

namespace View3D.Commands
{
    public interface ICommand
    {
        void Undo();
        void Execute();
        void Initialize(IComponentManager componentManager);
        string GetHintText();
        bool IsMutation();
    }


    public abstract class CommandBase<T> : ICommand
    {
        protected IComponentManager _componentManager;
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

        public virtual void Initialize(IComponentManager componentManager) 
        {
            _componentManager = componentManager;
        }

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
        public abstract string GetHintText();
        public virtual bool IsMutation() => true;
    }
}

