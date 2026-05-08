using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;

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
    }
}
