using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetSubDirectories : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetSubDirectories(Type containerType) : base(containerType) { }

        [Test]
        public void GetSubDirectories_Root_ReturnsImmediateSortedFolders()
        {
            var folders = _container.GetSubDirectories("");
            Assert.That(folders, Does.Contain("audio"));
            Assert.That(folders, Does.Contain("models"));

            var sorted = folders.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
            Assert.That(folders, Is.EqualTo(sorted));
        }

        [Test]
        public void GetSubDirectories_NestedFolder_ReturnsOnlyChildren()
        {
            var folders = _container.GetSubDirectories(@"models\textures");
            Assert.That(folders, Is.EqualTo(new[] { "specular" }));
        }

        [Test]
        public void GetSubDirectories_UnknownFolder_ReturnsEmpty()
        {
            Assert.That(_container.GetSubDirectories("missing\\folder"), Is.Empty);
        }
    }
}
