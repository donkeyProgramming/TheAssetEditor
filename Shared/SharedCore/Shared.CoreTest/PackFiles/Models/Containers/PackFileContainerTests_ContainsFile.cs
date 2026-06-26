using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_ContainsFile : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_ContainsFile(Type containerType) : base(containerType) { }

        [Test]
        public void ContainsFile_TrueForNormalizedInput()
        {
            Assert.That(_container.ContainsFile(" folder/file.txt "), Is.True);
        }

        [Test]
        public void ContainsFile_FalseForMissing()
        {
            Assert.That(_container.ContainsFile("missing.txt"), Is.False);
        }
    }
}
