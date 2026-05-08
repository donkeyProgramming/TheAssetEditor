using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Utility
{
    internal class PackFileServiceUtilityTests
    {
        [Test]
        public void SplitDirectoryEntries_ComposesSortedFoldersAndFiles()
        {
            var container = CreateContainer();

            var result = PackFileServiceUtility.SplitDirectoryEntries(container, "texture");

            Assert.That(result.DirectoryPath, Is.EqualTo("texture"));
            Assert.That(result.SubFolders, Is.EqualTo(new[] { "mesha", "meshb" }));
            Assert.That(result.Files.Select(x => x.FileName), Is.EqualTo(new[] { "alpha.dds", "texture_file.dds" }));
        }

        [Test]
        public void SplitDirectoryEntries_RootDirectory_ReturnsTopLevelFoldersAndFiles()
        {
            var container = CreateContainer();

            var result = PackFileServiceUtility.SplitDirectoryEntries(container, "");

            Assert.That(result.DirectoryPath, Is.EqualTo(string.Empty));
            Assert.That(result.SubFolders, Is.EqualTo(new[] { "audio", "texture" }));
            Assert.That(result.Files.Select(x => x.FileName), Is.EqualTo(new[] { "root.txt" }));
        }

        [Test]
        public void SplitDirectoryEntries_EmptyDirectory_ReturnsEmptyResult()
        {
            var container = CreateContainer();
            var result = PackFileServiceUtility.SplitDirectoryEntries(container, "missing");

            Assert.That(result.DirectoryPath, Is.EqualTo("missing"));
            Assert.That(result.SubFolders, Is.Empty);
            Assert.That(result.Files, Is.Empty);
        }

        private static PackFileContainer CreateContainer()
        {
            var container = new PackFileContainer("Test");
            var parent = new PackedFileSourceParent { FilePath = @"c:\game\p.pack" };

            container.AddOrUpdateFile(@"root.txt", new PackFile("root.txt", new PackedFileSource(parent, 0, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"audio\a.wem", new PackFile("a.wem", new PackedFileSource(parent, 1, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\texture_file.dds", new PackFile("texture_file.dds", new PackedFileSource(parent, 2, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\alpha.dds", new PackFile("alpha.dds", new PackedFileSource(parent, 3, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\mesha\filea", new PackFile("filea", new PackedFileSource(parent, 4, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\meshb\fileb", new PackFile("fileb", new PackedFileSource(parent, 5, 1, false, false, CompressionFormat.None, 0)));

            return container;
        }
    }
}
