using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_DeleteFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_DeleteFile(Type containerType) : base(containerType) { }

        [Test]
        public void DeleteFile_RemovesFileOrThrowsOnCached()
        {
            var file = _container.FindFile("folder\\file.txt")!;

            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() => _container.DeleteFile(file));
                return;
            }

            var deleted = _container.DeleteFile(file);
            Assert.That(deleted, Is.EqualTo(file));
            Assert.That(_container.ContainsFile("folder\\file.txt"), Is.False);
        }

        [Test]
        public void DeleteFile_UnknownFile_ReturnsNullForWritableContainer()
        {
            if (IsCachedContainer)
            {
                var known = _container.FindFile("folder\\file.txt")!;
                Assert.Throws<InvalidOperationException>(() => _container.DeleteFile(known));
                return;
            }

            Assert.That(_container.DeleteFile(new PackFile("x.txt", null)), Is.Null);
        }
    }
}
