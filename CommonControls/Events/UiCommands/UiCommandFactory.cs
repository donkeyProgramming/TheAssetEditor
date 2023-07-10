using Microsoft.Extensions.DependencyInjection;
using System;

namespace CommonControls.Events.UiCommands
{
    public interface IUiCommand
    {
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
            var instance = _serviceProvider.GetRequiredService<T>();
            if (configure != null)
                configure(instance);
            return instance;
        }
    }
}
