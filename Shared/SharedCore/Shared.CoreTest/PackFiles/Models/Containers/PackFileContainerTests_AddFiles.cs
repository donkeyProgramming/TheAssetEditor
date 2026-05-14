using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_AddFiles : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_AddFiles(Type containerType) : base(containerType) { }

        [Test]
        public void AddFiles_AddsMultipleFilesOrThrowsOnCached()
        {
            var newFiles = new List<NewPackFileEntry>
            {
                new("dir", new PackFile("a.txt", null)),
                new("", new PackFile("root.txt", null))
            };

            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() => _container.AddFiles(newFiles));
                return;
            }

            var added = _container.AddFiles(newFiles);
            Assert.That(added.Count, Is.EqualTo(2));
            Assert.That(_container.ContainsFile("dir\\a.txt"), Is.True);
            Assert.That(_container.ContainsFile("root.txt"), Is.True);
        }

        [Test]
        public void AddFiles_EmptyFileName_Throws()
        {
            if (IsCachedContainer)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _container.AddFiles([new NewPackFileEntry("dir", new PackFile("", null))]));
                return;
            }

            Assert.Throws<Exception>(() =>
                _container.AddFiles([new NewPackFileEntry("dir", new PackFile("", null))]));
        }
    }
}
