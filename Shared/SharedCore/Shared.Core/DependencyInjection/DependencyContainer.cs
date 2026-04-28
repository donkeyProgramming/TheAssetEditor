using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
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

        protected void RegisterGameComponent<T>(IServiceCollection serviceCollection) where T : class, IGameComponent
        {
            serviceCollection.AddScoped<T>();
            serviceCollection.AddScoped<IGameComponent, T>(x => x.GetRequiredService<T>());
        }

        protected void RegisterGameComponent<TInterface, TActual>(IServiceCollection serviceCollection) where TInterface : class, IGameComponent where TActual : class, TInterface
        {
            serviceCollection.AddScoped<TInterface, TActual>();
            serviceCollection.AddScoped<IGameComponent, TInterface>(x => x.GetRequiredService<TInterface>());
        }

        protected void RegisterWindow<TForm>(IServiceCollection serviceCollection) where TForm : Window
        {
            serviceCollection.AddTransient<TForm>();
            serviceCollection.AddScoped<Func<TForm>>(x => () => x.GetRequiredService<TForm>());
            serviceCollection.AddScoped<IAbstractFormFactory<TForm>, AbstractFormFactory<TForm>>();
        }
    }
}
