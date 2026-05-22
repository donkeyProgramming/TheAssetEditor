using Microsoft.Extensions.DependencyInjection;

namespace Shared.Core.Events
{
    public interface IAeCommand
    {
        void Execute();
    }

    public interface IAeUndoCommandCommand : IAeCommand
    {
        void Undo();
        string HintText { get; }
        bool IsMutation { get; }
    }

    public interface IUiCommandFactory
    {
        T Create<T>(Action<T>? configure = null) where T : IAeCommand;
        CommandBuilder<T> CreateWithBuilder<T>() where T : IAeUndoCommandCommand;
    }

    public class UiCommandFactory : IUiCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandManager _commandManager;

        public UiCommandFactory(IServiceProvider serviceProvider, CommandManager manager)
        {
            _serviceProvider = serviceProvider;
            _commandManager = manager;
        }

        public T Create<T>(Action<T>? configure = null) where T : IAeCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            configure?.Invoke(instance);
            return instance;
        }

        public CommandBuilder<T> CreateWithBuilder<T>() where T : IAeUndoCommandCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            return new CommandBuilder<T>(_commandManager, instance); ;
        }
    }
}
