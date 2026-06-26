using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_SaveToDisk : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SaveToDisk(Type containerType) : base(containerType) { }

        [Test]
        public void SaveToDisk_CachedContainer_Throws()
        {
            if (!IsCachedContainer)
                Assert.Ignore("Writable pack SaveToDisk behavior is validated in serializer/integration tests.");

            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => _container.SaveToDisk("path", false, gameInfo));
        }

        [Test]
        public void SaveToDisk_WritableContainer_ProducesValidPackFile()
        {
            if (IsCachedContainer)
                Assert.Ignore("CachedPackFileContainer does not support SaveToDisk.");

            if (!IsSystemFolderContainer)
                Assert.Ignore("PackFileContainer with PackedFileSource test data cannot SaveToDisk without a real pack file on disk.");

            var tempPath = Path.Combine(Path.GetTempPath(), "SaveToDiskTest_" + Guid.NewGuid().ToString("N") + ".pack");
            try
            {
                var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
                _container.SaveToDisk(tempPath, false, gameInfo);

                Assert.That(File.Exists(tempPath), Is.True);
                Assert.That(new FileInfo(tempPath).Length, Is.GreaterThan(0));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
