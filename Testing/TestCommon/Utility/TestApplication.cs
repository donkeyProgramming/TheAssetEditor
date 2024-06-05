using System;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using View3D;

namespace TestCommon.Utility
{
    public class TestApplication
    {
        DependencyContainer[] _dependencyContainers = new DependencyContainer[]
        {
            //new DependencyInjectionContainer(false),
            //new DependencyInjectionContainer(),
            new Test_DependencyContainer(),
        };

        IServiceProvider? _serviceProvider;

        public TestApplication()
        {
        }

        public T GetService<T>() where T : notnull => _serviceProvider!.GetRequiredService<T>();

        public TestApplication Build()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            RegisterTools(GetService<IToolFactory>());

            return this;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            foreach (var container in _dependencyContainers)
                container.Register(services);
        }

        void RegisterTools(IToolFactory factory)
        {
            foreach (var container in _dependencyContainers)
                container.RegisterTools(factory);
        }
    }

}
