using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    internal class PackFileContainerTests_ReadOnly : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_ReadOnly(Type containerType)
            : base(containerType)
        {
        }

        [Test]
        public void IsCaPackFile_AlwaysTrue()
        {
            Assert.That(_container.IsCaPackFile, Is.True);
        }

        [Test]
        public void IsCaPackFile_SetterDoesNotChangeValue()
        {
            _container.IsCaPackFile = false;
            if (IsCachedContainer)
                Assert.That(_container.IsCaPackFile, Is.True);
            else
                Assert.That(_container.IsCaPackFile, Is.False);
        }

        [Test]
        public void GetFileCount_ReturnsCorrectCount()
        {
            Assert.That(_container.GetFileCount(), Is.EqualTo(15));
        }

        [Test]
        public void FindFile_ReturnsFile()
        {
            var result = _container.FindFile("folder\\file.txt");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_ReturnsNullForMissing()
        {
            var result = _container.FindFile("missing\\path.txt");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindFile_NormalizesPath()
        {
            var result = _container.FindFile("FOLDER/FILE.TXT");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindFile_ReturnsCorrectDataSource()
        {
            var result = _container.FindFile("folder\\file.txt");
            var source = result!.DataSource as PackedFileSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Offset, Is.EqualTo(100));
            Assert.That(source.Size, Is.EqualTo(200));
        }

        [Test]
        public void ContainsFile_ReturnsTrueForExisting()
        {
            Assert.That(_container.ContainsFile("folder\\file.txt"), Is.True);
        }

        [Test]
        public void ContainsFile_ReturnsFalseForMissing()
        {
            Assert.That(_container.ContainsFile("missing.txt"), Is.False);
        }

        [Test]
        public void GetFullPath_ReturnsPath()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            var path = _container.GetFullPath(file);
            Assert.That(path, Is.EqualTo("folder\\file.txt"));
        }

        [Test]
        public void GetFullPath_ReturnsNullForMissing()
        {
            var unknownFile = new PackFile("unknown.txt", null);
            var path = _container.GetFullPath(unknownFile);
            Assert.That(path, Is.Null);
        }

        [Test]
        public void FindAllWithExtention_ReturnsMatching()
        {
            var results = _container.FindAllWithExtention(".wem");
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].FileName, Does.EndWith("sound.wem"));
        }

        [Test]
        public void FindAllWithExtention_ReturnsEmptyForNoMatch()
        {
            var results = _container.FindAllWithExtention(".xyz");
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void GetAllFiles_ReturnsAllFiles()
        {
            var all = _container.GetAllFiles();
            Assert.That(all.Count, Is.EqualTo(15));
            Assert.That(all.ContainsKey("folder\\file.txt"), Is.True);
            Assert.That(all.ContainsKey("other\\data.bin"), Is.True);
            Assert.That(all.ContainsKey("audio\\sound.wem"), Is.True);
        }

        [Test]
        public void AddFiles_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(AddFiles_Throws));
            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("new.txt", null))
            };
            Assert.Throws<InvalidOperationException>(() => cachedContainer.AddFiles(newFiles));
        }

        [Test]
        public void DeleteFile_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(DeleteFile_Throws));
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => cachedContainer.DeleteFile(file));
        }

        [Test]
        public void DeleteFolder_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(DeleteFolder_Throws));
            Assert.Throws<InvalidOperationException>(() => cachedContainer.DeleteFolder("folder"));
        }

        [Test]
        public void MoveFile_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(MoveFile_Throws));
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => cachedContainer.MoveFile(file, "other"));
        }

        [Test]
        public void RenameDirectory_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(RenameDirectory_Throws));
            Assert.Throws<InvalidOperationException>(() => cachedContainer.RenameDirectory("folder", "renamed"));
        }

        [Test]
        public void RenameFile_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(RenameFile_Throws));
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => cachedContainer.RenameFile(file, "renamed.txt"));
        }

        [Test]
        public void SaveFileData_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(SaveFileData_Throws));
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => cachedContainer.SaveFileData(file, [1, 2, 3]));
        }

        [Test]
        public void SaveToDisk_Throws()
        {
            var cachedContainer = GetCachedContainerOrIgnore(nameof(SaveToDisk_Throws));
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => cachedContainer.SaveToDisk("path", false, gameInfo));
        }

        [Test]
        public void AddOrUpdateFile_Throws()
        {
            IgnoreIfNotCached(nameof(AddOrUpdateFile_Throws));
            Assert.Throws<InvalidOperationException>(() =>
                _container.AddOrUpdateFile("test\\new.txt", new PackFile("new.txt", null)));
        }
    }
}
