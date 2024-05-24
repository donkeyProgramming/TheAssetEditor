using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Shared.Core.Misc
{
    public class DependencyContainer
    {
        public virtual void Register(IServiceCollection serviceCollection) { }

        public virtual void RegisterTools(IToolFactory factory) { }

        protected void RegisterAllAsOriginalType<T>(IServiceCollection serviceCollection, ServiceLifetime scope)
        {
            var implementations = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .ToList();

            foreach (var implementation in implementations)
                serviceCollection.Add(new ServiceDescriptor(implementation.UnderlyingSystemType, implementation, ServiceLifetime.Transient));
        }

        protected void RegisterAllAsInterface<T>(IServiceCollection serviceCollection, ServiceLifetime scope)
        {
            var implementations = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .ToList();

            foreach (var implementation in implementations)
            {
                serviceCollection.Add(new ServiceDescriptor(typeof(T), implementation, ServiceLifetime.Transient));
                //serviceCollection.Add(new ServiceDescriptor(implementation, ServiceLifetime.Transient));
            }
        }
    }
}
