using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_FindFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_FindFile(Type containerType) : base(containerType) { }

        [Test]
        public void FindFile_FindsExistingPath()
        {
            var file = _container.FindFile("folder\\file.txt");
            Assert.That(file, Is.Not.Null);
            Assert.That(file!.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_NormalizesCaseAndSlashes()
        {
            var file = _container.FindFile("FOLDER/FILE.TXT");
            Assert.That(file, Is.Not.Null);
        }

        [Test]
        public void FindFile_MissingPath_ReturnsNull()
        {
            Assert.That(_container.FindFile("missing\\file.txt"), Is.Null);
        }
    }
}
