using System;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Events.UiCommands
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
        T Create<T>(Action<T> configure = null) where T : IUiCommand;
    }

    public class UiCommandFactory : IUiCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UiCommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Action<T> configure = null) where T : IUiCommand
        {
            // TODO: REMOVE
            //Console.WriteLine("UICommandFactory.Create<T>");
            //Console.WriteLine($"Calling _serviceProvider.GetRequiredService<{typeof(T)}>()");
            //Console.WriteLine($"_serviceprovide = {_serviceProvider} ");
            var instance = _serviceProvider.GetRequiredService<T>();
            configure?.Invoke(instance);

            if (instance is IExecutableUiCommand executable)
                executable.Execute();
            return instance;
        }
    }
}
