using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_SaveFileData : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SaveFileData(Type containerType) : base(containerType) { }

        [Test]
        public void SaveFileData_UpdatesMemorySourceOrThrowsOnCached()
        {
            if (IsCachedContainer)
            {
                var cachedFile = _container.FindFile("folder\\file.txt")!;
                Assert.Throws<InvalidOperationException>(() => _container.SaveFileData(cachedFile, [1, 2, 3]));
                return;
            }

            if (IsSystemFolderContainer)
            {
                // SystemFolderContainer.AddOrUpdateFile creates a new PackFile internally,
                // so we must re-fetch the stored reference after adding.
                _container.AddOrUpdateFile("new.txt", new PackFile("new.txt", new MemorySource([1])));
                var storedFile = _container.FindFile("new.txt")!;
                _container.SaveFileData(storedFile, [9, 8, 7]);
                Assert.That(storedFile.DataSource.ReadData(), Is.EqualTo(new byte[] { 9, 8, 7 }));
                return;
            }

            var file = new PackFile("new.txt", new MemorySource([1]));
            _container.AddOrUpdateFile("new.txt", file);
            _container.SaveFileData(file, [9, 8, 7]);

            Assert.That(file.DataSource.ReadData(), Is.EqualTo(new byte[] { 9, 8, 7 }));
        }
    }
}
