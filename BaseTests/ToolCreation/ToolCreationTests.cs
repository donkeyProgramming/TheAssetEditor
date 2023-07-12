using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseTests.ToolCreation
{
    [TestClass]
    public class ToolCreationTests
    {
        IServiceProvider _serviceProvider;
        public ToolCreationTests()
        {
            var serviceCollection = new ServiceCollection();
            //ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            //RegisterTools(_serviceProvider.GetService<IToolFactory>());
        }


        [TestMethod]
        public void TestMethod1()
        {
            var toolFactory = _serviceProvider.GetRequiredService<IToolFactory>();

            var tool0 = toolFactory.Create("testformat.testformat");
            var tool1 = toolFactory.Create("testformat.testformat");

            // Check everything created


            // Check everything is dead 
            toolFactory.DestroyEditor(tool0);
            toolFactory.DestroyEditor(tool1);
        }
    }



}
