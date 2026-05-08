using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_Name : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_Name(Type containerType) : base(containerType) { }

        [Test]
        public void Name_IsSetFromContainerCreation()
        {
            Assert.That(_container.Name, Is.EqualTo("TestCache"));
        }
    }
}
