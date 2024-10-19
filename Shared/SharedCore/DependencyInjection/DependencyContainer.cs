using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;

namespace Shared.Core.DependencyInjection
{
    public class DependencyContainer
    {
        public virtual void Register(IServiceCollection serviceCollection) { }

        public virtual void RegisterTools(IEditorDatabase factory) { }

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

        protected void RegisterWindow<TForm>(IServiceCollection serviceCollection) where TForm : Window
        {
            serviceCollection.AddTransient<TForm>();
            serviceCollection.AddScoped<Func<TForm>>(x => () => x.GetRequiredService<TForm>());
            serviceCollection.AddScoped<IAbstractFormFactory<TForm>, AbstractFormFactory<TForm>>();
        }
    }
}
