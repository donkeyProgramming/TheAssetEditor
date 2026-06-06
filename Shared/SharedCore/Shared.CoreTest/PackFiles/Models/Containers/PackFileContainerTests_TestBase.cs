using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal abstract class PackFileContainerTests_TestBase
    {
        private readonly bool _useCachedContainer;

        protected IPackFileContainerInternal _container = null!;
        protected bool IsCachedContainer => _useCachedContainer;

        protected static readonly (string RelativePath, string FileName, long Offset, long Size, bool IsEncrypted, bool IsCompressed, CompressionFormat CompressionFormat, uint UncompressedSize)[] TestFiles =
        [
            ("folder\\file.txt", "file.txt", 100, 200, false, false, CompressionFormat.None, 0),
            ("other\\data.bin", "data.bin", 300, 400, false, true, CompressionFormat.Lz4, 800),
            ("audio\\sound.wem", "sound.wem", 700, 500, false, false, CompressionFormat.None, 0),
            ("root_file.txt", "root_file.txt", 0, 10, false, false, CompressionFormat.None, 0),
            ("models\\unit.model", "unit.model", 10, 20, false, false, CompressionFormat.None, 0),
            ("models\\vehicle.model", "vehicle.model", 30, 40, false, false, CompressionFormat.None, 0),
            ("models\\textures\\diffuse.dds", "diffuse.dds", 70, 50, false, false, CompressionFormat.None, 0),
            ("models\\textures\\normal.dds", "normal.dds", 120, 60, false, false, CompressionFormat.None, 0),
            ("models\\textures\\specular\\gloss.dds", "gloss.dds", 180, 30, false, false, CompressionFormat.None, 0),
            ("audio\\music.wem", "music.wem", 210, 100, false, false, CompressionFormat.None, 0),
            ("audio\\battle_sound.wem", "battle_sound.wem", 400, 300, false, false, CompressionFormat.None, 0),
            ("scripts\\campaign_script.lua", "campaign_script.lua", 850, 80, false, false, CompressionFormat.None, 0),
            ("folder_a\\shared.txt", "shared.txt", 900, 10, false, false, CompressionFormat.None, 0),
            ("folder_b\\shared.txt", "shared.txt", 910, 20, false, false, CompressionFormat.None, 0),
            ("compressed\\data.bin", "data.bin", 1000, 500, true, true, CompressionFormat.Lz4, 2000),
            ("audio\\voice.wem_temp", "voice.wem_temp", 1500, 10, false, false, CompressionFormat.None, 0),
            ("audio\\voice.wem.{sdf}", "voice.wem.{sdf}", 1510, 10, false, false, CompressionFormat.None, 0),
            ("audio\\voice.txt", "voice.txt", 1520, 10, false, false, CompressionFormat.None, 0),
        ];

        protected PackFileContainerTests_TestBase(Type containerType)
        {
            _useCachedContainer = containerType == typeof(CachedPackFileContainer);
        }

        [SetUp]
        public void Setup()
        {
            if (_useCachedContainer)
            {
                _container = CachedPackFileContainer.CreateFromFileList("TestCache", TestFiles, useInMemoryDb: true, systemFilePath: @"c:\game\data", sourcePackFilePath: @"c:\game\data\pack1.pack");
            }
            else
            {
                var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
                var sourceContainer = new PackFileContainer("TestCache")
                {
                    IsCaPackFile = true,
                    SystemFilePath = @"c:\game\data"
                };
                sourceContainer.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

                foreach (var file in TestFiles)
                    sourceContainer.AddOrUpdateFile(file.RelativePath, new PackFile(file.FileName, new PackedFileSource(parent, file.Offset, file.Size, file.IsEncrypted, file.IsCompressed, file.CompressionFormat, file.UncompressedSize)));

                _container = sourceContainer;
            }
        }

        [TearDown]
        public void TearDown()
        {
            (_container as IDisposable)?.Dispose();
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
