using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal class CachedPackFileContainerTests_PackFileSettings
    {
        [Test]
        public void CreateFromFileList_FileBackedCache_UsesDbPathAsSaveLocationAndPreservesGameVersion()
        {
            var dbFilePath = Path.Combine(Path.GetTempPath(), "CachedPackFileContainerTests_" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                using var container = CachedPackFileContainer.CreateFromFileList(
                    "TestCache",
                    PackFileContainerTests_TestBase.TestFiles,
                    useInMemoryDb: false,
                    dbFilePath: dbFilePath,
                    systemFilePath: @"c:\game\data",
                    sourcePackFilePath: @"c:\game\data\pack1.pack",
                    gameVersion: GameTypeEnum.Warhammer3);

                Assert.That(container.PackFileSettings.SaveLocationPath, Is.EqualTo(dbFilePath));
                Assert.That(container.PackFileSettings.GameVersion, Is.EqualTo(GameTypeEnum.Warhammer3));
            }
            finally
            {
                if (File.Exists(dbFilePath))
                    File.Delete(dbFilePath);
            }
        }
    }
}