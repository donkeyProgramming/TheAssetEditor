using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_AddOrUpdateFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_AddOrUpdateFile(Type containerType) : base(containerType) { }

        [Test]
        public void AddOrUpdateFile_NewPath_AddsFileOrThrowsOnCached()
        {
            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _container.AddOrUpdateFile("new\\file.txt", new PackFile("file.txt", new MemorySource([1]))));
                return;
            }

            _container.AddOrUpdateFile("new\\file.txt", new PackFile("file.txt", new MemorySource([1])));
            Assert.That(_container.ContainsFile("new\\file.txt"), Is.True);
        }

        [Test]
        public void AddOrUpdateFile_ExistingPath_UpdatesWithoutIncreasingCount()
        {
            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", new MemorySource([1]))));
                return;
            }

            var before = _container.GetFileCount();
            _container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", new MemorySource([2])));
            Assert.That(_container.GetFileCount(), Is.EqualTo(before));
        }
    }
}
