using System.Text;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Storage.CacheDatabase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Enums;

namespace Test.Audio
{
    [TestFixture]
    internal class AudioCacheTests
    {
        private string _tempDir = string.Empty;
        private string _dbFilePath = string.Empty;
        private List<SqliteConnection> _inMemoryKeepAliveConnections = [];

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AudioCacheTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _dbFilePath = Path.Combine(_tempDir, "test_cache.db");
            _inMemoryKeepAliveConnections = [];
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var connection in _inMemoryKeepAliveConnections)
                connection.Dispose();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private DbContextOptions<AudioCacheDbContext> CreateTestDbOptions()
        {
            var dbName = "AudioCacheTests_" + Guid.NewGuid().ToString("N");
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbName,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            var keepAliveConnection = new SqliteConnection(connectionString);
            keepAliveConnection.Open();
            _inMemoryKeepAliveConnections.Add(keepAliveConnection);

            return new DbContextOptionsBuilder<AudioCacheDbContext>()
                .UseSqlite(connectionString)
                .Options;
        }

        private DbContextOptions<AudioCacheDbContext> CreateFileDbOptions()
        {
            return new DbContextOptionsBuilder<AudioCacheDbContext>()
                .UseSqlite($"Data Source={_dbFilePath};Pooling=False")
                .Options;
        }

        private static void SaveCache(string fingerprint, List<IPackFileContainer> containers, DbContextOptions<AudioCacheDbContext> dbOptions)
        {
            var bnkLoader = new BnkLoader(new Mock<IPackFileService>().Object);
            using var cache = new AudioCache(dbOptions);
            cache.Save(fingerprint, true, containers, bnkLoader, new DatLoader.Result());
        }

        private static AudioCacheHelper CreateAudioCacheHelper()
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var packFileService = new Mock<IPackFileService>().Object;
            return new AudioCacheHelper(settings, new DatLoader(packFileService, settings), new BnkLoader(packFileService));
        }

        [Test]
        public void RoundTrip_PreservesBnkMetadata()
        {
            var bnk1 = PackFile.CreateFromBytes("bnk1.bnk", CreateBnk(1, 111, 0));
            var bnk2 = PackFile.CreateFromBytes("bnk2.bnk", CreateBnk(2, 222, 0));
            var container = CreateContainer(true, [(@"audio\wwise\bnk1.bnk", bnk1), (@"audio\wwise\bnk2.bnk", bnk2)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint123", [container.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint123");

            Assert.That(loaded, Is.Not.Null);
            var bnks = loaded.GetBnks();
            Assert.That(bnks, Has.Count.EqualTo(2));
            Assert.That(bnks.Single(x => x.Path == @"audio\wwise\bnk1.bnk").LanguageId, Is.EqualTo(111));
            Assert.That(bnks.Single(x => x.Path == @"audio\wwise\bnk2.bnk").LanguageId, Is.EqualTo(222));
            Assert.That(bnks.All(x => x.IsCA), Is.True);
        }

        [Test]
        public void LoadCache_ReturnsCorrectHircData()
        {
            const string BnkPath = @"audio\wwise\test.bnk";
            const uint EventId = 123456;
            var bnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(EventId, 678, 1024));
            var container = CreateContainer(true, [(BnkPath, bnk)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            var resolvedPaths = new HashSet<string>([BnkPath], StringComparer.OrdinalIgnoreCase);
            var hircs = loaded.FindHircs(EventId, resolvedPaths);

            Assert.That(hircs, Has.Count.EqualTo(1));
            var hirc = hircs.Single();
            Assert.That(hirc.Id, Is.EqualTo(EventId));
            Assert.That(hirc.HircType, Is.EqualTo(AkBkHircType.Event));
            Assert.That(hirc.BnkPath, Is.EqualTo(BnkPath));
            Assert.That(hirc.IsCA, Is.True);
            Assert.That(hirc.LanguageId, Is.EqualTo(678));

            var didx = loaded.FindDidx(resolvedPaths);
            Assert.That(didx.Single().Length, Is.EqualTo(1024));
        }

        [Test]
        public void LoadCache_PreservesDistinctBnkPathsAcrossHircs()
        {
            var bnk1 = PackFile.CreateFromBytes("bnk1.bnk", CreateBnk(1, 1, 0));
            var bnk2 = PackFile.CreateFromBytes("bnk2.bnk", CreateBnk(2, 1, 0));
            var container = CreateContainer(true, [(@"audio\wwise\bnk1.bnk", bnk1), (@"audio\wwise\bnk2.bnk", bnk2)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            var resolvedPaths = new HashSet<string>([@"audio\wwise\bnk1.bnk", @"audio\wwise\bnk2.bnk"], StringComparer.OrdinalIgnoreCase);
            var allHircs = loaded.FindAllHircs(resolvedPaths);

            Assert.That(allHircs.Single(x => x.Id == 1).BnkPath, Is.EqualTo(@"audio\wwise\bnk1.bnk"));
            Assert.That(allHircs.Single(x => x.Id == 2).BnkPath, Is.EqualTo(@"audio\wwise\bnk2.bnk"));
        }

        [Test]
        public void LoadCache_ReturnsNullForMissingFile()
        {
            var result = AudioCache.CreateFromFingerPrint(Path.Combine(_tempDir, "nonexistent.db"), "fingerprint");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void LoadCache_ReturnsNullForWrongFingerprint()
        {
            var container = CreateContainer(true, []);
            var dbOptions = CreateTestDbOptions();
            SaveCache("correctFingerprint", [container.Object], dbOptions);

            var result = AudioCache.CreateFromFingerPrint(dbOptions, "wrongFingerprint");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ComputeFingerprint_DeterministicForSameInputs()
        {
            var bnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(1, 1, 0));
            var container = CreateContainer(true, [(@"audio\wwise\test.bnk", bnk)]);
            var helper = CreateAudioCacheHelper();

            var firstFingerprint = helper.ComputeFingerprint([container.Object], "game files");
            var secondFingerprint = helper.ComputeFingerprint([container.Object], "game files");

            Assert.That(firstFingerprint, Is.EqualTo(secondFingerprint));
        }

        [Test]
        public void ComputeFingerprint_ChangesWhenFileChanges()
        {
            var helper = CreateAudioCacheHelper();
            var bnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(1, 1, 0));
            var container = CreateContainer(true, [(@"audio\wwise\test.bnk", bnk)]);
            var fingerprintBeforeChange = helper.ComputeFingerprint([container.Object], "game files");

            var modifiedBnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(2, 1, 0));
            var modifiedContainer = CreateContainer(true, [(@"audio\wwise\test.bnk", modifiedBnk)]);
            var fingerprintAfterChange = helper.ComputeFingerprint([modifiedContainer.Object], "game files");

            Assert.That(fingerprintBeforeChange, Is.Not.EqualTo(fingerprintAfterChange));
        }

        [Test]
        public void ComputeFingerprint_FileEnumerationOrderIndependent()
        {
            var bnk1 = PackFile.CreateFromBytes("bnk1.bnk", CreateBnk(1, 1, 0));
            var bnk2 = PackFile.CreateFromBytes("bnk2.bnk", CreateBnk(2, 1, 0));
            var containerForward = CreateContainer(true, [(@"audio\wwise\bnk1.bnk", bnk1), (@"audio\wwise\bnk2.bnk", bnk2)]);
            var containerReversed = CreateContainer(true, [(@"audio\wwise\bnk2.bnk", bnk2), (@"audio\wwise\bnk1.bnk", bnk1)]);
            var helper = CreateAudioCacheHelper();

            var forwardFingerprint = helper.ComputeFingerprint([containerForward.Object], "game files");
            var reversedFingerprint = helper.ComputeFingerprint([containerReversed.Object], "game files");

            Assert.That(forwardFingerprint, Is.EqualTo(reversedFingerprint));
        }

        [Test]
        public void ComputeFingerprint_MissingBackingFile()
        {
            var packParent = new PackedFileSourceParent { FilePath = Path.Combine(_tempDir, "missing.pack") };
            var bnk = new PackFile("missing.bnk", new PackedFileSource(packParent, 0, 100, false, false, CompressionFormat.None, 0));
            var container = CreateContainer(true, [(@"audio\wwise\missing.bnk", bnk)]);

            var fingerprint = CreateAudioCacheHelper().ComputeFingerprint([container.Object], "game files");

            Assert.That(fingerprint, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GetCacheFilePath_SanitizesInvalidChars()
        {
            var path = CreateAudioCacheHelper().GetCacheFilePath("Game:Name/With<Bad>Chars", "abc123");
            var fileName = Path.GetFileName(path);

            Assert.That(fileName.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
            Assert.That(path.EndsWith(".db"), Is.True);
        }

        [Test]
        public void RoundTrip_FullCycle()
        {
            const string BnkPath = @"audio\wwise\test.bnk";
            const uint EventId = 999;
            var bnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(EventId, 1, 2048));
            var container = CreateContainer(true, [(BnkPath, bnk)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);
            using var restored = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            Assert.That(restored, Is.Not.Null);
            var resolvedPaths = new HashSet<string>([BnkPath], StringComparer.OrdinalIgnoreCase);

            Assert.Multiple(() =>
            {
                Assert.That(restored.GetBnks(), Has.Count.EqualTo(1));
                Assert.That(restored.FindHircs(EventId, resolvedPaths), Has.Count.EqualTo(1));
                Assert.That(restored.FindAllHircs(resolvedPaths), Has.Count.EqualTo(1));
                Assert.That(restored.FindDidx(resolvedPaths).Single().Length, Is.EqualTo(2048));
                Assert.That(restored.FindHircIds(1, true, resolvedPaths), Does.Contain(EventId));
            });
        }

        [Test]
        public void SaveAndLoadCache_ReturnsQueryableCache()
        {
            const string bnkPath = @"audio\wwise\test.bnk";
            const uint eventId = 555;
            var bnk = PackFile.CreateFromBytes("test.bnk", CreateBnk(eventId, 1, 0));
            var container = CreateContainer(true, [(bnkPath, bnk)]);
            var packFileService = new Mock<IPackFileService>();
            packFileService.Setup(x => x.GetAllPackfileContainers()).Returns([container.Object]);
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var helper = new AudioCacheHelper(settings, new DatLoader(packFileService.Object, settings), new BnkLoader(packFileService.Object));
            var source = new AudioCacheSource(_dbFilePath, "fingerprint", true, [container.Object], [container.Object]);

            using var cache = helper.SaveAndLoadCache(source);

            Assert.That(File.Exists(_dbFilePath), Is.True);
            Assert.That(cache.FindHircs(eventId, new HashSet<string>([bnkPath], StringComparer.OrdinalIgnoreCase)), Has.Count.EqualTo(1));
        }

        [Test]
        public void TryLoadFromCache_ReturnsCacheWhenValid()
        {
            var container = CreateContainer(true, []);
            var dbOptions = CreateFileDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);

            var result = CreateAudioCacheHelper().TryLoadFromCache(_dbFilePath, "fingerprint");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TryLoadFromCache_ReturnsNullForMissingFile()
        {
            var result = CreateAudioCacheHelper().TryLoadFromCache(Path.Combine(_tempDir, "does_not_exist.db"), "fingerprint");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryLoadFromCache_ReturnsNullForCorruptFile()
        {
            File.WriteAllBytes(_dbFilePath, [0xFF, 0xFE, 0x00, 0x01]);
            var result = CreateAudioCacheHelper().TryLoadFromCache(_dbFilePath, "fingerprint");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SaveCache_PreservesIsCAFlag()
        {
            var vanillaBnk = PackFile.CreateFromBytes("vanilla.bnk", CreateBnk(1, 1, 0));
            var moddedBnk = PackFile.CreateFromBytes("modded.bnk", CreateBnk(2, 1, 0));
            var vanillaContainer = CreateContainer(true, [(@"audio\wwise\vanilla.bnk", vanillaBnk)]);
            var moddedContainer = CreateContainer(false, [(@"audio\wwise\modded.bnk", moddedBnk)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [vanillaContainer.Object, moddedContainer.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            var bnks = loaded.GetBnks();
            Assert.That(bnks.Single(x => x.Path.Contains("vanilla")).IsCA, Is.True);
            Assert.That(bnks.Single(x => x.Path.Contains("modded")).IsCA, Is.False);
        }

        [Test]
        public void SaveCache_NoBnks_RoundTrips()
        {
            var container = CreateContainer(true, []);
            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.GetBnks(), Is.Empty);
        }

        [Test]
        public void SaveCache_OverwritesExistingCache()
        {
            var dbOptions = CreateFileDbOptions();

            var bnk1 = PackFile.CreateFromBytes("bnk1.bnk", CreateBnk(1, 1, 0));
            var container1 = CreateContainer(true, [(@"audio\wwise\bnk1.bnk", bnk1)]);
            SaveCache("firstFingerprint", [container1.Object], dbOptions);

            var bnk2 = PackFile.CreateFromBytes("bnk2.bnk", CreateBnk(2, 1, 0));
            var container2 = CreateContainer(true, [(@"audio\wwise\bnk2.bnk", bnk2)]);
            SaveCache("secondFingerprint", [container2.Object], dbOptions);

            var oldResult = AudioCache.CreateFromFingerPrint(dbOptions, "firstFingerprint");
            Assert.That(oldResult, Is.Null);

            using var newResult = AudioCache.CreateFromFingerPrint(dbOptions, "secondFingerprint");
            Assert.That(newResult, Is.Not.Null);
            Assert.That(newResult.GetBnks().Single().Path, Is.EqualTo(@"audio\wwise\bnk2.bnk"));
        }

        [Test]
        public void SaveCache_IncludesInMemoryBackedBnks()
        {
            // Freshly generated bnks are MemorySource-backed until saved to disk, so unlike the packfile cache these must still be indexable.
            var bnk = new PackFile("generated.bnk", new MemorySource(CreateBnk(1, 1, 0)));
            var container = CreateContainer(false, [(@"audio\wwise\generated.bnk", bnk)]);

            var dbOptions = CreateTestDbOptions();
            SaveCache("fingerprint", [container.Object], dbOptions);
            using var loaded = AudioCache.CreateFromFingerPrint(dbOptions, "fingerprint");

            Assert.That(loaded.GetBnks(), Has.Count.EqualTo(1));
        }

        [Test]
        public void MergeDatData_AppendsLayerDataAndLetsProjectNamesOverrideGameNames()
        {
            var gameData = new CachedAudioDatData
            {
                NameById = new Dictionary<uint, string> { [1] = "game", [2] = "game-only" },
                StateGroupsByDialogueEvent = new Dictionary<string, List<string>> { ["event"] = ["game-group"] },
                StatesByStateGroup = new Dictionary<string, List<string>> { ["group"] = ["game-state"] }
            };
            var projectData = new CachedAudioDatData
            {
                NameById = new Dictionary<uint, string> { [1] = "project", [3] = "project-only" },
                StateGroupsByDialogueEvent = new Dictionary<string, List<string>> { ["event"] = ["project-group"] },
                StatesByStateGroup = new Dictionary<string, List<string>> { ["group"] = ["project-state"] }
            };

            var result = AudioRepository.MergeDatData([gameData, projectData]);

            Assert.Multiple(() =>
            {
                Assert.That(result.NameById[1], Is.EqualTo("project"));
                Assert.That(result.NameById[2], Is.EqualTo("game-only"));
                Assert.That(result.NameById[3], Is.EqualTo("project-only"));
                Assert.That(result.StateGroupsByDialogueEvent["event"], Is.EqualTo(new[] { "game-group", "project-group" }));
                Assert.That(result.StatesByStateGroup["group"], Is.EqualTo(new[] { "game-state", "project-state" }));
            });
        }

        [Test]
        public void CreateCacheSources_ProjectCacheUsesOnlyProjectContainers()
        {
            var gameContainer = CreateContainer(true, []);
            gameContainer.SetupGet(x => x.Name).Returns("game");
            var projectBnk = PackFile.CreateFromBytes("project.bnk", CreateBnk(1, 1, 0));
            var projectContainer = CreateContainer(false, [(@"audio\wwise\project.bnk", projectBnk)]);
            projectContainer.SetupGet(x => x.Name).Returns("project");

            var packFileService = new Mock<IPackFileService>();
            packFileService.Setup(x => x.GetAllPackfileContainers()).Returns([gameContainer.Object, projectContainer.Object]);
            packFileService.Setup(x => x.GetEditablePack()).Returns(projectContainer.Object);

            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var bnkLoader = new BnkLoader(packFileService.Object);
            var cacheHelper = new AudioCacheHelper(settings, new DatLoader(packFileService.Object, settings), bnkLoader);
            var eventHub = new Mock<IEventHub>();
            var repository = new AudioRepository(settings, packFileService.Object, cacheHelper, bnkLoader, eventHub.Object);

            var sources = repository.CreateCacheSources();

            Assert.Multiple(() =>
            {
                Assert.That(sources, Has.Count.EqualTo(2));
                Assert.That(sources[1].BnkContainers, Is.EqualTo(new[] { projectContainer.Object }));
                Assert.That(sources[1].DatContainers, Is.EqualTo(new[] { projectContainer.Object }));
                Assert.That(sources[1].Fingerprint, Is.EqualTo(cacheHelper.ComputeFingerprint([projectContainer.Object], "project files")));
            });
        }

        private static Mock<IPackFileContainer> CreateContainer(bool isCa, List<(string Path, PackFile File)> files)
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.IsCaPackFile).Returns(isCa);
            container.SetupGet(x => x.ContainerType).Returns(PackFileContainerType.Normal);
            container
                .Setup(
                    x => x.SearchFiles(
                        It.IsAny<string?>(),
                        It.IsAny<IReadOnlyList<string>?>()))
                .Returns(
                    (string? _, IReadOnlyList<string>? extensions) =>
                        files
                            .Where(
                                x => extensions == null
                                    || extensions.Contains(
                                        Path.GetExtension(x.Path),
                                        StringComparer.OrdinalIgnoreCase))
                            .ToList());
            return container;
        }

        private static byte[] CreateBnk(uint eventId, uint languageId, int embeddedMediaSize, bool includeHirc = true)
        {
            using var bnk = new MemoryStream();

            using (var header = new MemoryStream())
            using (var writer = new BinaryWriter(header, Encoding.UTF8, true))
            {
                writer.Write((uint)2147483784);
                writer.Write((uint)55);
                writer.Write(languageId);
                writer.Write((uint)0);
                writer.Write((uint)999);
                WriteChunk(bnk, "BKHD", header.ToArray());
            }

            if (includeHirc)
            {
                using var hirc = new MemoryStream();
                using var writer = new BinaryWriter(hirc, Encoding.UTF8, true);
                writer.Write((uint)1);
                writer.Write((byte)AkBkHircType.Event);
                writer.Write((uint)5);
                writer.Write(eventId);
                writer.Write((byte)0);
                WriteChunk(bnk, "HIRC", hirc.ToArray());
            }

            if (embeddedMediaSize > 0)
            {
                using var didx = new MemoryStream();
                using (var writer = new BinaryWriter(didx, Encoding.UTF8, true))
                {
                    writer.Write((uint)987);
                    writer.Write((uint)0);
                    writer.Write((uint)embeddedMediaSize);
                }
                WriteChunk(bnk, "DIDX", didx.ToArray());
                WriteChunk(bnk, "DATA", new byte[embeddedMediaSize]);
            }

            return bnk.ToArray();
        }

        private static void WriteChunk(Stream output, string tag, byte[] payload)
        {
            output.Write(Encoding.ASCII.GetBytes(tag));
            using var writer = new BinaryWriter(output, Encoding.UTF8, true);
            writer.Write((uint)payload.Length);
            output.Write(payload);
        }
    }
}
