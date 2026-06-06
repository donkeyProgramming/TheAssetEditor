using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_Read
    {
        private string _tempDir = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;
        private SystemFolderContainer _container = null!;

        // Test file structure:
        //   folder/file.txt
        //   folder/sub/nested.bin
        //   root_file.txt
        //   models/unit.model

        private static readonly string[] TestRelativePaths =
        [
            @"folder\file.txt",
            @"folder\sub\nested.bin",
            "root_file.txt",
            @"models\unit.model",
        ];

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SystemFolderContainerTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Create real files on disk so FileSystemSource can read sizes
            foreach (var rel in TestRelativePaths)
            {
                var abs = Path.Combine(_tempDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
                File.WriteAllText(abs, $"content of {rel}");
            }

            // Set up mock to return the file list
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(_tempDir)).Returns(true);
            _fileSystemAccess
                .Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns(TestRelativePaths.Select(r => Path.Combine(_tempDir, r)).ToArray());

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
        public void Constructor_ScansFolder_PopulatesFileList()
        {
            Assert.That(_container.GetFileCount(), Is.EqualTo(TestRelativePaths.Length));
        }

        [Test]
        public void Constructor_SetsNameToFolderName()
        {
            var expectedName = Path.GetFileName(_tempDir);
            Assert.That(_container.Name, Is.EqualTo(expectedName));
        }

        [Test]
        public void Constructor_SetsSystemFilePath()
        {
            Assert.That(_container.SystemFilePath, Is.EqualTo(_tempDir));
        }

        [Test]
        public void Constructor_IsCaPackFile_IsFalse()
        {
            Assert.That(_container.IsCaPackFile, Is.False);
        }

        [Test]
        public void Constructor_EmptyPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SystemFolderContainer("", _fileSystemAccess.Object));
        }

        [Test]
        public void Constructor_NonExistentDirectory_Throws()
        {
            var fs = new Mock<IFileSystemAccess>();
            fs.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);

            Assert.Throws<DirectoryNotFoundException>(() => new SystemFolderContainer(@"C:\nonexistent", fs.Object));
        }

        [Test]
        public void FindFile_NormalizedPath_ReturnsPackFile()
        {
            var result = _container.FindFile(@"folder\file.txt");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_CaseInsensitive_ReturnsPackFile()
        {
            var result = _container.FindFile(@"FOLDER\FILE.TXT");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_NonExistent_ReturnsNull()
        {
            var result = _container.FindFile(@"does\not\exist.txt");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ContainsFile_ExistingFile_ReturnsTrue()
        {
            Assert.That(_container.ContainsFile(@"root_file.txt"), Is.True);
        }

        [Test]
        public void ContainsFile_MissingFile_ReturnsFalse()
        {
            Assert.That(_container.ContainsFile(@"missing.txt"), Is.False);
        }

        [Test]
        public void GetAllFiles_ReturnsAllScannedFiles()
        {
            var all = _container.GetAllFiles();
            Assert.That(all.Count, Is.EqualTo(TestRelativePaths.Length));
        }

        [Test]
        public void GetFullPath_ReturnsCorrectRelativePath()
        {
            var file = _container.FindFile(@"models\unit.model");
            Assert.That(file, Is.Not.Null);

            var path = _container.GetFullPath(file!);
            Assert.That(path, Is.EqualTo(@"models\unit.model"));
        }

        [Test]
        public void GetFullPath_UnknownFile_ReturnsNull()
        {
            var unknownFile = new PackFile("unknown.txt", null!);
            var path = _container.GetFullPath(unknownFile);
            Assert.That(path, Is.Null);
        }

        [Test]
        public void GetAllFilesByFolder_GroupsCorrectly()
        {
            var byFolder = _container.GetAllFilesByFolder();

            Assert.That(byFolder.ContainsKey("folder"), Is.True);
            Assert.That(byFolder["folder"], Does.Contain("file.txt"));

            Assert.That(byFolder.ContainsKey(@"folder\sub"), Is.True);
            Assert.That(byFolder[@"folder\sub"], Does.Contain("nested.bin"));

            Assert.That(byFolder.ContainsKey(string.Empty), Is.True);
            Assert.That(byFolder[string.Empty], Does.Contain("root_file.txt"));

            Assert.That(byFolder.ContainsKey("models"), Is.True);
            Assert.That(byFolder["models"], Does.Contain("unit.model"));
        }

        [Test]
        public void FindAllWithExtention_ReturnsMatchingFiles()
        {
            var results = _container.FindAllWithExtention(".txt");
            Assert.That(results.Count, Is.EqualTo(2)); // folder\file.txt + root_file.txt
            Assert.That(results.All(r => r.FileName.EndsWith(".txt")), Is.True);
        }

        [Test]
        public void FindAllWithExtention_NoMatch_ReturnsEmpty()
        {
            var results = _container.FindAllWithExtention(".xyz");
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void SearchFiles_FilterByName_ReturnsMatch()
        {
            var results = _container.SearchFiles("nested", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("nested.bin"));
        }

        [Test]
        public void SearchFiles_FilterByExtension_ReturnsMatch()
        {
            var results = _container.SearchFiles(null, [".model"]);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.model"));
        }

        [Test]
        public void SearchFiles_NoFilter_ReturnsAll()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(TestRelativePaths.Length));
        }

        [Test]
        public void GetDirectoryContent_RootLevel_ReturnsRootFiles()
        {
            var results = _container.GetDirectoryContent("");
            Assert.That(results.Count, Is.EqualTo(1)); // root_file.txt
            Assert.That(results[0].File.Name, Is.EqualTo("root_file.txt"));
        }

        [Test]
        public void GetDirectoryContent_SubFolder_ReturnsDirectChildren()
        {
            var results = _container.GetDirectoryContent("folder");
            Assert.That(results.Count, Is.EqualTo(1)); // folder\file.txt (not nested)
            Assert.That(results[0].File.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void Dispose_ClearsFileList()
        {
            _container.Dispose();
            Assert.That(_container.GetFileCount(), Is.EqualTo(0));
        }
    }
}
