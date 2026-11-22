using System.Text;
using Shared.ByteParsing;
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

        internal static PackFileContainer LoadFromDisk(string packPath)
        {
            using var stream = File.OpenRead(packPath);
            using var reader = new BinaryReader(stream, Encoding.ASCII);
            return PackFileSerializerLoader.Load(packPath, stream.Length, reader, new CaPackDuplicateFileResolver());
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
