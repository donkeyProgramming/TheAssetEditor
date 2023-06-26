using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Linq;
using System;
using System.Collections.Generic;
using View3D.Components.Component.Selection;
using System.Data.Common;
using View3D.Components.Component;
using static CommonControls.Editors.AnimationPack.Converters.AnimationBinWh3FileToXmlConverter;

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


    public class Tester
    {
        public void Test()
        {
            CommandFactory factory = new CommandFactory(null, null);

            factory.Create<MyCommand>()
                .Configure(x=> x.Configure("sdf", 123))
                .IsUndoable(false)
                .BuildAndExecute();
        }
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

    public class MyCommand : CommandBase<MyCommand>
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public MyCommand()
        {
        }



        public override string GetHintText() => "";

        protected override void ExecuteCommand()
        {
            throw new NotImplementedException();
        }

        protected override void UndoCommand()
        {
            throw new NotImplementedException();
        }

        internal void Configure(string v1, int v2)
        {
            throw new NotImplementedException();
        }
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

