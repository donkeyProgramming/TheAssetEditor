using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_AddOrUpdateFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_AddOrUpdateFile(Type containerType) : base(containerType) { }

        [Test]
        public void AddOrUpdateFile_NewPath_AddsFileOrThrowsOnCached()
        {
            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _container.AddOrUpdateFile("new\\file.txt", new PackFile("file.txt", null)));
                return;
            }

            _container.AddOrUpdateFile("new\\file.txt", new PackFile("file.txt", null));
            Assert.That(_container.ContainsFile("new\\file.txt"), Is.True);
        }

        [Test]
        public void AddOrUpdateFile_ExistingPath_UpdatesWithoutIncreasingCount()
        {
            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", null)));
                return;
            }

            var before = _container.GetFileCount();
            _container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", null));
            Assert.That(_container.GetFileCount(), Is.EqualTo(before));
        }
    }
}
