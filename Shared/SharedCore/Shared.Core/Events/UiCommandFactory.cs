using Microsoft.Extensions.DependencyInjection;

namespace Shared.Core.Events
{
    public interface IUiCommand
    {
    }

    public interface IExecutableUiCommand : IUiCommand
    {
        public void Execute();
    }

    public interface IUiCommandFactory
    {
        T Create<T>(Action<T>? configure = null) where T : IUiCommand;
    }

    public class UiCommandFactory : IUiCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UiCommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Action<T>? configure = null) where T : IUiCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            configure?.Invoke(instance);

            if (instance is IExecutableUiCommand executable)
                executable.Execute();
            return instance;
        }
    }
}
