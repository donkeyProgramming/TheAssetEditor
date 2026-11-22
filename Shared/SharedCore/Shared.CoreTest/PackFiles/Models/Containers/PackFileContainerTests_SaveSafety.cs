using System.Text;
using Shared.ByteParsing;
using Shared.Core.PackFiles.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    // Saving a pack that was opened from disk, where its files still read from that same file
    [TestFixture]
    internal class PackFileContainerTests_SaveSafety
    {
        private static readonly GameInformation s_wh3 = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);

        private static readonly (string Path, string Content)[] s_files =
        [
            (@"db\units_tables\mymod", new string('D', 300)),
            (@"text\db\mymod.loc", new string('L', 200)),
            (@"variantmeshes\unit.rigid_model_v2", new string('M', 500)),
            (@"variantmeshes\unit.wsmodel", new string('W', 400)),
        ];

        private string _dir = null!;
        private string _packPath = null!;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PackSaveSafety_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _packPath = Path.Combine(_dir, "mymod.pack");

            var seed = PackFileContainer.CreatePackFile("mymod", _packPath, PackFileVersion.PFH5);
            foreach (var (path, content) in s_files)
                seed.AddOrUpdateFile(path, PackFile.CreateFromASCII(Path.GetFileName(path), content));
            seed.SaveToDisk(_packPath, false, s_wh3);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        [Test]
        public void SaveToDisk_FailsPartway_FilesStillReadTheOriginalPack()
        {
            var pack = LoadFromDisk(_packPath);
            var bytesBefore = File.ReadAllBytes(_packPath);

            // Sorts after every other file, so they have all been written to the temp file before this throws
            pack.AddOrUpdateFile(@"zzz\broken.txt", new PackFile("broken.txt", new ThrowingSource()));

            Assert.Throws<IOException>(() => pack.SaveToDisk(_packPath, false, s_wh3));

            Assert.That(File.ReadAllBytes(_packPath), Is.EqualTo(bytesBefore));
            Assert.That(File.Exists(_packPath + "_temp"), Is.False);
            foreach (var (path, content) in s_files)
                Assert.That(ReadText(pack, path), Is.EqualTo(content), path);
        }

        [Test]
        public void SaveToDisk_Succeeds_FilesReadTheNewPack()
        {
            var pack = LoadFromDisk(_packPath);
            pack.AddOrUpdateFile(@"variantmeshes\new.txt", PackFile.CreateFromASCII("new.txt", "NEW"));

            pack.SaveToDisk(_packPath, false, s_wh3);

            foreach (var (path, content) in s_files)
                Assert.That(ReadText(pack, path), Is.EqualTo(content), path);
            Assert.That(ReadText(pack, @"variantmeshes\new.txt"), Is.EqualTo("NEW"));

            var reloaded = LoadFromDisk(_packPath);
            Assert.That(reloaded.GetFileCount(), Is.EqualTo(s_files.Length + 1));
        }

        [Test]
        public void PackRewrittenOnDisk_SameLength_ReadsAndSaveRefuse()
        {
            var pack = LoadFromDisk(_packPath);
            pack.AddOrUpdateFile(@"variantmeshes\edited.txt", PackFile.CreateFromASCII("edited.txt", "EDIT"));

            // Another program saves the pack: same length, so the old size-only check let this through
            var rewritten = File.ReadAllBytes(_packPath);
            rewritten[^1] ^= 0xFF;
            File.WriteAllBytes(_packPath, rewritten);
            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(1));

            var file = pack.FindFile(s_files[0].Path)!;
            Assert.Throws<PackFileChangedOnDiskException>(() => file.DataSource.ReadData());
            Assert.Throws<PackFileChangedOnDiskException>(() => pack.SaveToDisk(_packPath, false, s_wh3));
            Assert.That(File.ReadAllBytes(_packPath), Is.EqualTo(rewritten));
        }

        [Test]
        public void PackRewrittenOnDisk_UntrackedPack_StillReads()
        {
            // CA packs and other containers that never asked for tracking are not checked
            var pack = LoadFromDisk(_packPath, trackChanges: false);
            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(1));

            Assert.That(ReadText(pack, s_files[0].Path), Is.EqualTo(s_files[0].Content));
        }

        [Test]
        public void SaveToDisk_UntouchedFiles_StoredBytesUnchanged_EditedFilesFollowCompressionRules()
        {
            var pack = LoadFromDisk(_packPath);
            var storedBefore = s_files.ToDictionary(x => x.Path, x => StoredBytes(pack, x.Path));

            // An uncompressed entry, as RPFM stores most files; AE's rules would compress a .lua
            var blobPath = Path.Combine(_dir, "blob.bin");
            var luaContent = new string('S', 600);
            File.WriteAllText(blobPath, luaContent);
            var blobParent = new PackedFileSourceParent { FilePath = blobPath };
            pack.AddOrUpdateFile(@"script\mymod.lua", new PackFile("mymod.lua", new PackedFileSource(blobParent, 0, luaContent.Length, false, false, CompressionFormat.None, 0)));

            pack.AddOrUpdateFile(@"script\edited.lua", PackFile.CreateFromASCII("edited.lua", new string('E', 600)));
            pack.SaveToDisk(_packPath, false, s_wh3);

            var reloaded = LoadFromDisk(_packPath);
            foreach (var (path, _) in s_files)
                Assert.That(StoredBytes(reloaded, path), Is.EqualTo(storedBefore[path]), path);

            var lua = (PackedFileSource)reloaded.FindFile(@"script\mymod.lua")!.DataSource;
            Assert.That(lua.IsCompressed, Is.False);
            Assert.That(ReadText(reloaded, @"script\mymod.lua"), Is.EqualTo(luaContent));

            var edited = (PackedFileSource)reloaded.FindFile(@"script\edited.lua")!.DataSource;
            Assert.That(edited.CompressionFormat, Is.EqualTo(CompressionFormat.Zstd));
        }

        [Test]
        public void SaveToDisk_ForGameWithoutTheStoredFormat_ConvertsCompression()
        {
            var pack = LoadFromDisk(_packPath);
            var wh3Formats = s_files
                .Select(x => ((PackedFileSource)pack.FindFile(x.Path)!.DataSource).CompressionFormat)
                .ToList();
            Assert.That(wh3Formats, Has.Some.EqualTo(CompressionFormat.Zstd).Or.Some.EqualTo(CompressionFormat.Lz4));

            var wh2PackPath = Path.Combine(_dir, "mymod_wh2.pack");
            pack.SaveToDisk(wh2PackPath, false, GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer2));

            var reloaded = LoadFromDisk(wh2PackPath);
            foreach (var (path, content) in s_files)
            {
                var format = ((PackedFileSource)reloaded.FindFile(path)!.DataSource).CompressionFormat;
                Assert.That(format, Is.AnyOf(CompressionFormat.None, CompressionFormat.Lzma1), path);
                Assert.That(ReadText(reloaded, path), Is.EqualTo(content), path);
            }
        }

        private static byte[] StoredBytes(PackFileContainer pack, string path)
        {
            return ((PackedFileSource)pack.FindFile(path)!.DataSource).ReadDataWithoutDecompressing();
        }

        // Mirrors PackFileContainerLoader.CreateFromPackFile
        internal static PackFileContainer LoadFromDisk(string packPath, bool trackChanges = true)
        {
            PackFileContainer pack;
            using (var stream = File.OpenRead(packPath))
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
                pack = PackFileSerializerLoader.Load(packPath, stream.Length, reader, new CaPackDuplicateFileResolver());

            if (trackChanges)
                pack.TrackChangesOnDisk();
            return pack;
        }

        internal static string ReadText(PackFileContainer pack, string path)
        {
            var file = pack.FindFile(path);
            Assert.That(file, Is.Not.Null, path);
            return Encoding.ASCII.GetString(file!.DataSource.ReadData());
        }

        internal class ThrowingSource : IDataSource
        {
            public long Size => 10;
            public CompressionFormat CompressionFormat => CompressionFormat.None;
            public byte[] ReadData() => throw new IOException("Simulated read failure");
            public byte[] PeekData(int size) => throw new IOException("Simulated read failure");
            public ByteChunk ReadDataAsChunk() => throw new IOException("Simulated read failure");
        }
    }
}
