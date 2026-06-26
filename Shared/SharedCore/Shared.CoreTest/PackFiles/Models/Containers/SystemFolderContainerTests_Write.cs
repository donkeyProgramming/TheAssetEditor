using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_Write
    {
        private string _tempDir = null!;
        private SystemFolderContainer _container = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderWrite_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Seed with one file
            var seedPath = Path.Combine(_tempDir, "existing", "seed.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(seedPath)!);
            File.WriteAllText(seedPath, "seed content");

            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(_tempDir)).Returns(true);
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([seedPath]);

            // Default pass-through for directory/file operations
            _fileSystemAccess.Setup(x => x.DirectoryExists(It.IsAny<string>()))
                .Returns((string p) => Directory.Exists(p));
            _fileSystemAccess.Setup(x => x.DirectoryCreateDirectory(It.IsAny<string>()))
                .Callback((string p) => Directory.CreateDirectory(p));
            _fileSystemAccess.Setup(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string p, byte[] d) => { Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllBytes(p, d); });
            _fileSystemAccess.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns((string p) => File.Exists(p));
            _fileSystemAccess.Setup(x => x.FileDelete(It.IsAny<string>()))
                .Callback((string p) => File.Delete(p));
            _fileSystemAccess.Setup(x => x.FileMove(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string s, string d) => { Directory.CreateDirectory(Path.GetDirectoryName(d)!); File.Move(s, d); });
            _fileSystemAccess.Setup(x => x.DirectoryMove(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string s, string d) => Directory.Move(s, d));
            _fileSystemAccess.Setup(x => x.DirectoryDelete(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((string p, bool r) => Directory.Delete(p, r));

            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void AddOrUpdateFile_WritesFileToDisk()
        {
            var data = "hello world"u8.ToArray();
            var file = new PackFile("new.txt", new MemorySource(data));

            _container.AddOrUpdateFile(@"folder\new.txt", file);

            var absolutePath = Path.Combine(_tempDir, "folder", "new.txt");
            Assert.That(File.Exists(absolutePath), Is.True);
            Assert.That(File.ReadAllBytes(absolutePath), Is.EqualTo(data));
        }

        [Test]
        public void AddOrUpdateFile_UpdatesFileList()
        {
            var data = "test"u8.ToArray();
            var file = new PackFile("new.txt", new MemorySource(data));

            _container.AddOrUpdateFile(@"folder\new.txt", file);

            Assert.That(_container.ContainsFile(@"folder\new.txt"), Is.True);
        }

        [Test]
        public void AddOrUpdateFile_EmptyName_Throws()
        {
            var file = new PackFile("", new MemorySource([1, 2, 3]));
            Assert.Throws<Exception>(() => _container.AddOrUpdateFile(@"folder\", file));
        }

        [Test]
        public void AddOrUpdateFile_SuppressesWatcher()
        {
            var data = "test"u8.ToArray();
            var file = new PackFile("new.txt", new MemorySource(data));

            bool wasSuppressed = false;
            _fileSystemAccess.Setup(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string p, byte[] d) => { wasSuppressed = _container._suppressWatcher; File.WriteAllBytes(p, d); });

            _container.AddOrUpdateFile(@"test\new.txt", file);

            Assert.That(wasSuppressed, Is.True);
            Assert.That(_container._suppressWatcher, Is.False); // restored after
        }

        [Test]
        public void AddFiles_MultipleFiles_AllWrittenToDisk()
        {
            var newFiles = new List<NewPackFileEntry>
            {
                new("dir", new PackFile("a.txt", new MemorySource("aaa"u8.ToArray()))),
                new("", new PackFile("root.txt", new MemorySource("bbb"u8.ToArray()))),
            };

            var added = _container.AddFiles(newFiles);

            Assert.That(added.Count, Is.EqualTo(2));
            Assert.That(_container.ContainsFile(@"dir\a.txt"), Is.True);
            Assert.That(_container.ContainsFile("root.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "dir", "a.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "root.txt")), Is.True);
        }

        [Test]
        public void AddFiles_EmptyFileName_Throws()
        {
            var newFiles = new List<NewPackFileEntry>
            {
                new("dir", new PackFile("", new MemorySource([1]))),
            };

            Assert.Throws<Exception>(() => _container.AddFiles(newFiles));
        }

        [Test]
        public void DeleteFile_RemovesFromDiskAndFileList()
        {
            var seedFile = _container.FindFile(@"existing\seed.txt");
            Assert.That(seedFile, Is.Not.Null);

            var result = _container.DeleteFile(seedFile!);

            Assert.That(result, Is.Not.Null);
            Assert.That(_container.ContainsFile(@"existing\seed.txt"), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "existing", "seed.txt")), Is.False);
        }

        [Test]
        public void DeleteFile_NonExistentFile_ReturnsNull()
        {
            var unknownFile = new PackFile("nope.txt", new MemorySource([1]));
            var result = _container.DeleteFile(unknownFile);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void DeleteFolder_RemovesRecursively()
        {
            // Add more files in a folder
            var data = "x"u8.ToArray();
            _container.AddOrUpdateFile(@"todelete\a.txt", new PackFile("a.txt", new MemorySource(data)));
            _container.AddOrUpdateFile(@"todelete\sub\b.txt", new PackFile("b.txt", new MemorySource(data)));

            _container.DeleteFolder("todelete");

            Assert.That(_container.ContainsFile(@"todelete\a.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"todelete\sub\b.txt"), Is.False);
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "todelete")), Is.False);
        }

        [Test]
        public void MoveFile_UpdatesDiskAndFileList()
        {
            var file = _container.FindFile(@"existing\seed.txt")!;
            _container.MoveFile(file, "newfolder");

            Assert.That(_container.ContainsFile(@"existing\seed.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"newfolder\seed.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "newfolder", "seed.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "existing", "seed.txt")), Is.False);
        }

        [Test]
        public void RenameFile_UpdatesDiskAndFileList()
        {
            var file = _container.FindFile(@"existing\seed.txt")!;
            _container.RenameFile(file, "renamed.txt");

            Assert.That(_container.ContainsFile(@"existing\seed.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"existing\renamed.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "existing", "renamed.txt")), Is.True);
        }

        [Test]
        public void RenameDirectory_UpdatesAllChildPaths()
        {
            // Add files in a directory
            var data = "x"u8.ToArray();
            _container.AddOrUpdateFile(@"olddir\a.txt", new PackFile("a.txt", new MemorySource(data)));
            _container.AddOrUpdateFile(@"olddir\sub\b.txt", new PackFile("b.txt", new MemorySource(data)));

            _container.RenameDirectory("olddir", "newdir");

            Assert.That(_container.ContainsFile(@"olddir\a.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"olddir\sub\b.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"newdir\a.txt"), Is.True);
            Assert.That(_container.ContainsFile(@"newdir\sub\b.txt"), Is.True);
        }

        [Test]
        public void SaveFileData_WritesNewContent()
        {
            var file = _container.FindFile(@"existing\seed.txt")!;
            var newData = "updated content"u8.ToArray();

            _container.SaveFileData(file, newData);

            var absolutePath = Path.Combine(_tempDir, "existing", "seed.txt");
            Assert.That(File.ReadAllBytes(absolutePath), Is.EqualTo(newData));
        }
    }
}
