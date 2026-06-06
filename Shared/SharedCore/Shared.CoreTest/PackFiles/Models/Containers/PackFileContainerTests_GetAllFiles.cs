using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_GetAllFiles : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetAllFiles(Type containerType) : base(containerType) { }

        [Test]
        public void GetAllFiles_ReturnsExpectedCountAndKeys()
        {
            var files = _container.GetAllFiles();
            Assert.That(files.Count, Is.EqualTo(18));
            Assert.That(files.ContainsKey("folder\\file.txt"), Is.True);
            Assert.That(files.ContainsKey("scripts\\campaign_script.lua"), Is.True);
        }

        [Test]
        public void GetAllFiles_PreservesCompressionMetadata()
        {
            var source = (PackedFileSource)_container.GetAllFiles()["compressed\\data.bin"].DataSource;
            Assert.That(source.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(source.UncompressedSize, Is.EqualTo(2000));
        }
    }
}
