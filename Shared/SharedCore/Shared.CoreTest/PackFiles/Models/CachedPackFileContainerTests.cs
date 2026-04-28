using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models
{
    internal class CachedPackFileContainer_ReadOnly
    {
        private CachedPackFileContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new CachedPackFileContainer("TestCache")
            {
                SystemFilePath = @"c:\game\data"
            };
            _container.FileList["folder\\file.txt"] = new PackFile("file.txt", null);
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
            Assert.That(_container.IsCaPackFile, Is.True);
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
        public void GetFullPath_ReturnsPath()
        {
            var file = _container.FileList["folder\\file.txt"];
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
        public void AddFiles_Throws()
        {
            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("new.txt", null))
            };
            Assert.Throws<InvalidOperationException>(() => _container.AddFiles(newFiles));
        }

        [Test]
        public void DeleteFile_Throws()
        {
            var file = _container.FileList["folder\\file.txt"];
            Assert.Throws<InvalidOperationException>(() => _container.DeleteFile(file));
        }

        [Test]
        public void DeleteFolder_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _container.DeleteFolder("folder"));
        }

        [Test]
        public void MoveFile_Throws()
        {
            var file = _container.FileList["folder\\file.txt"];
            Assert.Throws<InvalidOperationException>(() => _container.MoveFile(file, "other"));
        }

        [Test]
        public void RenameDirectory_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _container.RenameDirectory("folder", "renamed"));
        }

        [Test]
        public void RenameFile_Throws()
        {
            var file = _container.FileList["folder\\file.txt"];
            Assert.Throws<InvalidOperationException>(() => _container.RenameFile(file, "renamed.txt"));
        }

        [Test]
        public void SaveFileData_Throws()
        {
            var file = _container.FileList["folder\\file.txt"];
            Assert.Throws<InvalidOperationException>(() => _container.SaveFileData(file, [1, 2, 3]));
        }

        [Test]
        public void SaveToDisk_Throws()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => _container.SaveToDisk("path", false, gameInfo));
        }
    }
}
