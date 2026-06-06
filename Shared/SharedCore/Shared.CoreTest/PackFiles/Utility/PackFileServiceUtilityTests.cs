using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Utility
{
    internal class PackFileServiceUtilityTests
    {
        [Test]
        public void GetDirectoryContent_ReturnsSortedFiles()
        {
            var container = CreateContainer();

            var files = container.GetDirectoryContent("texture")
                .Select(x => x.File.Name)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Assert.That(files, Is.EqualTo(new[] { "alpha.dds", "texture_file.dds" }));
        }



        [Test]
        public void GetDirectoryContent_RootDirectory_ReturnsTopLevelFiles()
        {
            var container = CreateContainer();

            var files = container.GetDirectoryContent("")
                .Select(x => x.File.Name)
                .ToList();

            Assert.That(files, Is.EqualTo(new[] { "root.txt" }));
        }



        [Test]
        public void GetDirectoryContent_MissingDirectory_ReturnsEmpty()
        {
            var container = CreateContainer();

            Assert.That(container.GetDirectoryContent("missing"), Is.Empty);
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
