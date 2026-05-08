using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetDirectoryContent : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetDirectoryContent(Type containerType) : base(containerType) { }

        [Test]
        public void GetDirectoryContent_Root_ReturnsDirectFilesOnly()
        {
            var rows = _container.GetDirectoryContent("");
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Path, Is.EqualTo("root_file.txt"));
        }

        [Test]
        public void GetDirectoryContent_NestedFolder_ReturnsOnlyItsFiles()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            Assert.That(rows.Select(x => x.Path), Is.EquivalentTo(new[]
            {
                @"models\textures\diffuse.dds",
                @"models\textures\normal.dds"
            }));
        }

        [Test]
        public void GetDirectoryContent_UnknownFolder_ReturnsEmpty()
        {
            Assert.That(_container.GetDirectoryContent("missing\\folder"), Is.Empty);
        }

        [Test]
        public void GetDirectoryContent_ComposesWithUtility()
        {
            var split = PackFileServiceUtility.SplitDirectoryEntries(_container, "models");
            Assert.That(split.Files.Any(x => x.FileName == "unit.model"), Is.True);
            Assert.That(split.SubFolders, Does.Contain("textures"));
        }
    }
}
