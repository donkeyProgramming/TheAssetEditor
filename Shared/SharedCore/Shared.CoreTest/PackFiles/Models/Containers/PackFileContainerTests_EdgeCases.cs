using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_EdgeCases : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_EdgeCases(Type containerType)
            : base(containerType)
        {
        }

        [Test]
        public void FindAllWithExtention_IsCaseInsensitive()
        {
            var upper = _container.FindAllWithExtention(".WEM");
            var lower = _container.FindAllWithExtention(".wem");
            Assert.That(upper.Count, Is.EqualTo(3));
            Assert.That(lower.Count, Is.EqualTo(3));
        }

        [Test]
        public void ContainsFile_NormalizesForwardSlashes()
        {
            Assert.That(_container.ContainsFile("folder/file.txt"), Is.True);
            Assert.That(_container.ContainsFile("FOLDER/FILE.TXT"), Is.True);
            Assert.That(_container.ContainsFile(" folder\\file.txt "), Is.True);
        }

        [Test]
        public void GetAllFiles_PreservesCompressionMetadata()
        {
            var all = _container.GetAllFiles();
            var source = (PackedFileSource)all["compressed\\data.bin"].DataSource;

            Assert.That(source.IsEncrypted, Is.True);
            Assert.That(source.IsCompressed, Is.True);
            Assert.That(source.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(source.UncompressedSize, Is.EqualTo(2000));
            Assert.That(source.Offset, Is.EqualTo(1000));
            Assert.That(source.Size, Is.EqualTo(500));
        }

        [Test]
        public void GetFullPath_WithDuplicateFileNames_ReturnsFirstMatch()
        {
            var file = _container.FindFile("folder_a\\shared.txt")!;
            var path = _container.GetFullPath(file);

            Assert.That(path, Does.Contain("shared.txt"));
            Assert.That(path, Does.Contain("\\"));
        }

        [Test]
        public void GetDirectoryContent_UnknownFolder_ReturnsEmpty()
        {
            Assert.That(_container.GetDirectoryContent("missing\\folder"), Is.Empty);
            Assert.That(_container.GetSubDirectories("missing\\folder"), Is.Empty);
        }
    }
}
