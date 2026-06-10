using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_Name : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_Name(Type containerType) : base(containerType) { }

        [Test]
        public void Name_IsSetFromContainerCreation()
        {
            if (IsSystemFolderContainer)
            {
                Assert.That(_container.Name, Does.StartWith("PackFileContainerTests_"));
                return;
            }

            Assert.That(_container.Name, Is.EqualTo("TestCache"));
        }
    }
}
