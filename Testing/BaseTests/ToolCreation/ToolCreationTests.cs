using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCommon.Utility;

namespace BaseTests.ToolCreation
{
    [TestClass]
    public class ToolCreationTests
    {
        TestApplication _application;
        public ToolCreationTests()
        {
            _application = new TestApplication().Build();
        }

        [TestMethod]
        public void CreateAndDestroyTool()
        {
            var toolFactory = _application.GetService<IToolFactory>();
            var scopeRepo = _application.GetService<ScopeRepository>();

            var tool0 = toolFactory.Create("file0.testformat") as TestEditor;
            var tool1 = toolFactory.Create("file1.testformat") as TestEditor;

            Assert.IsNotNull(tool0);
            Assert.IsNotNull(tool1);

            Assert.AreNotEqual(tool0, tool1);
            Assert.AreNotEqual(tool0.EventHub, tool1.EventHub);
            Assert.AreEqual(2, scopeRepo.Scopes.Count());

            // Remove tool 0
            toolFactory.DestroyEditor(tool0);
            Assert.IsTrue(tool0.IsDisposed);
            Assert.IsTrue(tool0.EventHub.IsDisposed);
            Assert.AreEqual(1, scopeRepo.Scopes.Count());

            // Remove tool 1
            toolFactory.DestroyEditor(tool1);
            Assert.IsTrue(tool1.IsDisposed);
            Assert.IsTrue(tool1.EventHub.IsDisposed);
            Assert.AreEqual(0, scopeRepo.Scopes.Count());
        }
    }

}
