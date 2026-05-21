using Microsoft.Extensions.DependencyInjection;

namespace Shared.Core.Events
{
    public interface IAeCommand
    {
    }

    public interface IAeUndoCommandCommand : IAeCommand
    {
        void Undo();
        void Execute();
        string HintText { get; }
        bool IsMutation { get; }
    }


    public record CommandStackChangedEvent(string HintText, bool IsMutation);
    public record CommandStackUndoEvent(string HintText);



    public class CommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandExecutor _commandExecutor;

        public CommandFactory(IServiceProvider serviceProvider, CommandExecutor commandExecutor)
        {
            _serviceProvider = serviceProvider;
            _commandExecutor = commandExecutor;
        }

        public CommandBuilder<T> Create<T>() where T : IAeUndoCommandCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            return new CommandBuilder<T>(_commandExecutor, instance); ;
        }
    }

    public interface IUiCommandFactory
    {
        T Create<T>(Action<T>? configure = null) where T : IAeCommand;
    }



    public class UiCommandFactory : IUiCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UiCommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Action<T>? configure = null) where T : IAeCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            configure?.Invoke(instance);
            return instance;
        }
    }
}
