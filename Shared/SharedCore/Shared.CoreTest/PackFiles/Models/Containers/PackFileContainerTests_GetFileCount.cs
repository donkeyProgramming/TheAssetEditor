using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_GetFileCount : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetFileCount(Type containerType) : base(containerType) { }

        [Test]
        public void GetFileCount_ReturnsMasterDatasetCount()
        {
            Assert.That(_container.GetFileCount(), Is.EqualTo(18));
        }
    }
}
