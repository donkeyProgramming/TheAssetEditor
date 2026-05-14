using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetFullPath : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetFullPath(Type containerType) : base(containerType) { }

        [Test]
        public void GetFullPath_ReturnsStoredPath()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            var path = _container.GetFullPath(file);
            Assert.That(path, Is.EqualTo("folder\\file.txt"));
        }

        [Test]
        public void GetFullPath_UnknownFile_ReturnsNull()
        {
            var unknown = new PackFile("none.txt", null);
            Assert.That(_container.GetFullPath(unknown), Is.Null);
        }

        [Test]
        public void GetFullPath_DuplicateFileName_ReturnsPathForExactSource()
        {
            var file = _container.FindFile("folder_b\\shared.txt")!;

            var path = _container.GetFullPath(file);

            Assert.That(path, Is.EqualTo("folder_b\\shared.txt"));
        }

        [Test]
        public void GetFullPath_DuplicateFileName_WithUnknownSource_ReturnsNullForCachedContainer()
        {
            IgnoreIfNotCached("Ambiguous filename fallback");

            var unknownSourceFile = PackFile.CreateFromBytes("shared.txt", [1, 2, 3]);

            var path = _container.GetFullPath(unknownSourceFile);

            Assert.That(path, Is.Null);
        }

        [Test]
        public void GetFullPath_UniqueFileName_WithUnknownSource_UsesNameFallback()
        {
            var unknownSourceFile = PackFile.CreateFromBytes("file.txt", [1, 2, 3]);

            var path = _container.GetFullPath(unknownSourceFile);

            Assert.That(path, Is.EqualTo("folder\\file.txt"));
        }
    }
}
