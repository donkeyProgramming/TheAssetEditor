using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetDirectoryContent_PathRows : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetDirectoryContent_PathRows(Type containerType)
            : base(containerType)
        {
        }

        [Test]
        public void Root_IncludesOnlyRootFiles()
        {
            var rows = _container.GetDirectoryContent("");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Contain("root_file.txt"));
        }

        [Test]
        public void Root_ExcludesDeepDescendantFolders()
        {
            var rows = _container.GetDirectoryContent("");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Not.Contain(@"texture\mesha\filea"));
            Assert.That(paths, Does.Not.Contain(@"texture\meshb\filea"));
        }

        [Test]
        public void Root_ReturnsExpectedRowCount()
        {
            var rows = _container.GetDirectoryContent("");
            Assert.That(rows.Count, Is.EqualTo(1));
        }

        [Test]
        public void Subfolder_IncludesOnlyDirectFiles()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Contain(@"models\textures\diffuse.dds"));
            Assert.That(paths, Does.Contain(@"models\textures\normal.dds"));
            Assert.That(paths.Count, Is.EqualTo(2));
        }

        [Test]
        public void Subfolder_ExcludesParentAndSiblingRows()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths.Any(p => p.StartsWith(@"audio\", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(paths.Any(p => !p.StartsWith(@"models\textures\", StringComparison.OrdinalIgnoreCase)), Is.False);
        }

        [Test]
        public void Subfolder_ReturnsExpectedRowCount()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            Assert.That(rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void LeafFolder_ReturnsOnlyDirectFiles()
        {
            var rows = _container.GetDirectoryContent(@"audio");
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows.All(r => r.Path.StartsWith(@"audio\", StringComparison.OrdinalIgnoreCase)), Is.True);
        }
    }
}
