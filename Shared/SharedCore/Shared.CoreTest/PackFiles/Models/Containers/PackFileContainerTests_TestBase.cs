using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal abstract class PackFileContainerTests_TestBase
    {
        private readonly bool _useCachedContainer;

        private SqliteConnection? _cacheKeepAliveConnection;
        protected IPackFileContainerInternal _container = null!;
        protected bool IsCachedContainer => _useCachedContainer;

        protected PackFileContainerTests_TestBase(Type containerType)
        {
            _useCachedContainer = containerType == typeof(CachedPackFileContainer);
        }

        [SetUp]
        public void Setup()
        {
            var sourceContainer = new PackFileContainer("TestCache")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };
            sourceContainer.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            sourceContainer.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", new PackedFileSource(parent, 100, 200, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("other\\data.bin", new PackFile("data.bin", new PackedFileSource(parent, 300, 400, false, true, CompressionFormat.Lz4, 800)));
            sourceContainer.AddOrUpdateFile("audio\\sound.wem", new PackFile("sound.wem", new PackedFileSource(parent, 700, 500, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("root_file.txt", new PackFile("root_file.txt",new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\unit.model", new PackFile("unit.model",  new PackedFileSource(parent, 10, 20, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\vehicle.model", new PackFile("vehicle.model", new PackedFileSource(parent, 30, 40, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\diffuse.dds", new PackFile("diffuse.dds",new PackedFileSource(parent, 70, 50, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\normal.dds", new PackFile("normal.dds", new PackedFileSource(parent, 120, 60, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\specular\\gloss.dds", new PackFile("gloss.dds",new PackedFileSource(parent, 180, 30, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\music.wem", new PackFile("music.wem", new PackedFileSource(parent, 210, 100, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\battle_sound.wem", new PackFile("battle_sound.wem", new PackedFileSource(parent, 400, 300, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("scripts\\campaign_script.lua", new PackFile("campaign_script.lua",new PackedFileSource(parent, 850, 80, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("folder_a\\shared.txt", new PackFile("shared.txt",new PackedFileSource(parent, 900, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("folder_b\\shared.txt", new PackFile("shared.txt", new PackedFileSource(parent, 910, 20, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("compressed\\data.bin", new PackFile("data.bin", new PackedFileSource(parent, 1000, 500, true, true, CompressionFormat.Lz4, 2000)));

            if (_useCachedContainer)
            {
                var dbName = "CachedContainerTests_" + Guid.NewGuid().ToString("N");
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbName,
                    Mode = SqliteOpenMode.Memory,
                    Cache = SqliteCacheMode.Shared
                }.ToString();

                _cacheKeepAliveConnection = new SqliteConnection(connectionString);
                _cacheKeepAliveConnection.Open();

                var dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                var cached = new CachedPackFileContainer("test_cache", dbOptions);
                cached.Save("test_fp", sourceContainer);
                _container = CachedPackFileContainer.CreateFromFingerPrint(dbOptions, "test_fp")!;
            }
            else
            {
                _container = sourceContainer;
            }
        }

        [TearDown]
        public void TearDown()
        {
            _cacheKeepAliveConnection?.Dispose();
            _cacheKeepAliveConnection = null;
        }

        protected void IgnoreIfNotCached(string scenario)
        {
            if (!IsCachedContainer)
                Assert.Ignore($"{scenario} is currently validated only for CachedPackFileContainer.");
        }

        protected CachedPackFileContainer GetCachedContainerOrIgnore(string scenario)
        {
            if (_container is CachedPackFileContainer cachedContainer)
                return cachedContainer;

            Assert.Ignore($"{scenario} requires CachedPackFileContainer-specific API not present on IPackFileContainerInternal.");
            return null!;
        }
    }
}
