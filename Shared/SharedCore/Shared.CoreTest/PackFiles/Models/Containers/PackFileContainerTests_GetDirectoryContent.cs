using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetDirectoryContent : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetDirectoryContent(Type containerType)
            : base(containerType)
        {
        }

        [Test]
        public void Root_ReturnsRootFilesAndTopFolders()
        {
            var content = PackFileServiceUtility.SplitDirectoryEntries(_container, "");

            Assert.That(content.Files.Any(f => f.FileName == "root_file.txt"), Is.True);
            Assert.That(content.SubFolders, Does.Contain("models"));
            Assert.That(content.SubFolders, Does.Contain("audio"));
            Assert.That(content.SubFolders, Does.Not.Contain("textures"));
        }

        [Test]
        public void Root_DoesNotIncludeFilesFromSubfolders()
        {
            var entries = _container.GetDirectoryContent("");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].File.Name, Is.EqualTo("root_file.txt"));
        }

        [Test]
        public void Subfolder_ReturnsDirectFilesAndImmediateSubfolders()
        {
            var entries = _container.GetDirectoryContent("models");
            var subFolders = _container.GetSubDirectories("models");

            Assert.That(entries.Any(f => f.File.Name == "unit.model"), Is.True);
            Assert.That(entries.Any(f => f.File.Name == "vehicle.model"), Is.True);
            Assert.That(subFolders, Does.Contain("textures"));
            Assert.That(entries.Any(f => f.File.Name == "diffuse.dds"), Is.False);
        }

        [Test]
        public void NestedSubfolder_ReturnsCorrectContent()
        {
            var entries = _container.GetDirectoryContent("models\\textures");
            var subFolders = _container.GetSubDirectories("models\\textures");

            Assert.That(entries.Any(f => f.File.Name == "diffuse.dds"), Is.True);
            Assert.That(entries.Any(f => f.File.Name == "normal.dds"), Is.True);
            Assert.That(subFolders, Does.Contain("specular"));
            Assert.That(entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void DeepNestedFolder_ReturnsOnlyItsFiles()
        {
            var entries = _container.GetDirectoryContent("models\\textures\\specular");
            var subFolders = _container.GetSubDirectories("models\\textures\\specular");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].File.Name, Is.EqualTo("gloss.dds"));
            Assert.That(subFolders, Is.Empty);
        }

        [Test]
        public void NonexistentFolder_ReturnsEmpty()
        {
            var entries = _container.GetDirectoryContent("nonexistent\\path");
            var subFolders = _container.GetSubDirectories("nonexistent\\path");

            Assert.That(entries, Is.Empty);
            Assert.That(subFolders, Is.Empty);
        }

        [Test]
        public void SubfoldersAreSorted()
        {
            var subFolders = _container.GetSubDirectories("");

            var sorted = subFolders.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
            Assert.That(subFolders, Is.EqualTo(sorted));
        }

        [Test]
        public void FilesAreSorted()
        {
            var entries = _container.GetDirectoryContent("models");

            var sorted = entries.OrderBy(x => x.File.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            Assert.That(entries.Select(f => f.File.Name), Is.EqualTo(sorted.Select(f => f.File.Name)));
        }

        [Test]
        public void Files_HaveCorrectDataSource()
        {
            var entries = _container.GetDirectoryContent("models");
            var unitFile = entries.First(f => f.File.Name == "unit.model");
            var source = unitFile.File.DataSource as PackedFileSource;

            Assert.That(source, Is.Not.Null);
            Assert.That(source.Offset, Is.EqualTo(10));
            Assert.That(source.Size, Is.EqualTo(20));
            Assert.That(source.Parent.FilePath, Is.EqualTo(@"c:\game\data\pack1.pack"));
        }
    }
}
