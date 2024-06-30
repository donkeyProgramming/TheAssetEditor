using System;
using AssetEditor.Services;
using KitbasherEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace E2EVerification
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var _serviceProvider = new DependencyInjectionConfig().Build();
            var _rootScope = _serviceProvider.CreateScope();


            var kitbasher = _rootScope.ServiceProvider.GetRequiredService<KitbasherViewModel>();
            //kitbasher.MainFile 
        }
    }
}
