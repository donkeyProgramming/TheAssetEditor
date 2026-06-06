using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Microsoft.Data.Sqlite;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_SearchFiles : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_SearchFiles(Type containerType) : base(containerType) { }

        [Test]
        public void SearchFiles_NullFilters_ReturnsAll()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(15));
        }

        [Test]
        public void SearchFiles_TextAndExtension_FiltersCorrectly()
        {
            var results = _container.SearchFiles("battle", [".wem"]);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Path, Is.EqualTo(@"audio\battle_sound.wem"));
        }

        [Test]
        public void SearchFiles_ResultsArePathSorted()
        {
            var paths = _container.SearchFiles(null, null).Select(x => x.Path).ToList();
            var sorted = paths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(paths, Is.EqualTo(sorted));
        }

        [Test]
        public void SearchFiles_EmptyExtensionList_TreatedAsNoFilter()
        {
            var results = _container.SearchFiles(null, []);
            Assert.That(results.Count, Is.EqualTo(15));
        }

        [Test]
        public void SearchFiles_ExtensionFilter_MatchesFilenameSubstrings_ForLegacyWemVariants()
        {
            var sourceContainer = new PackFileContainer("SearchFilesVariants")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };
            sourceContainer.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            sourceContainer.AddOrUpdateFile("audio\\voice.wem", new PackFile("voice.wem", new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\voice.wem_temp", new PackFile("voice.wem_temp", new PackedFileSource(parent, 10, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\voice.wem.{sdf}", new PackFile("voice.wem.{sdf}", new PackedFileSource(parent, 20, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\voice.txt", new PackFile("voice.txt", new PackedFileSource(parent, 30, 10, false, false, CompressionFormat.None, 0)));

            SqliteConnection? keepAliveConnection = null;
            IPackFileContainer container;
            try
            {
                if (IsCachedContainer)
                    {
                    var dbName = "SearchFilesVariants_" + Guid.NewGuid().ToString("N");
                    var connectionString = new SqliteConnectionStringBuilder
                    {
                        DataSource = dbName,
                        Mode = SqliteOpenMode.Memory,
                        Cache = SqliteCacheMode.Shared
                    }.ToString();

                    // Keep one connection alive so the shared in-memory DB remains available
                    // while SaveCache and LoadContainerFromCache use separate EF/raw connections.
                    keepAliveConnection = new SqliteConnection(connectionString);
                    keepAliveConnection.Open();

                    var cacheHelper = new PackFileContainerCacheHelper();
                    var dbOptions = cacheHelper.CreateDbOptionsFromConnectionString(connectionString);
                    cacheHelper.SaveCache("variants_fp", sourceContainer, dbOptions);
                    container = cacheHelper.LoadContainerFromCache(dbOptions, "variants_fp")!;
                }
                else
                {
                    container = sourceContainer;
                }

                var results = container.SearchFiles(null, [".wem"]);
                var paths = results.Select(x => x.Path).ToList();

                Assert.That(paths, Does.Contain(@"audio\voice.wem"));
                Assert.That(paths, Does.Contain(@"audio\voice.wem_temp"));
                Assert.That(paths, Does.Contain(@"audio\voice.wem.{sdf}"));
                Assert.That(paths, Does.Not.Contain(@"audio\voice.txt"));
            }
            finally
            {
                keepAliveConnection?.Dispose();
            }
        }
    }
}
