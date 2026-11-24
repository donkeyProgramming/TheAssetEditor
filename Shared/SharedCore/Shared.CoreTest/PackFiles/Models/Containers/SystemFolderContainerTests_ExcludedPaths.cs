using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    // A folder project's ExcludedPaths (in aeproject.json, .git by default) are not loaded: not in the tree, not watched,
    // not packed. A project folder kept in git used to show its whole .git folder and pack it.
    [TestFixture]
    internal class SystemFolderContainerTests_ExcludedPaths
    {
        string _tempDir = null!;
        readonly Mock<IFileSystemWatcher> _watcher = new();

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderExcluded_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            Write(@"db\units.tsv");
            Write(@".git\HEAD");
            Write(@".git\objects\ab\cdef");
            Write(".gitignore");
            Write(@".github\workflow.yml");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        void Write(string relativePath)
        {
            var path = Path.Combine(_tempDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, relativePath);
        }

        SystemFolderContainer Open() => new(_tempDir, new FileSystemAccess(), _watcher.Object);

        [Test]
        public void TheGitFolder_IsNotLoaded_ButNamesThatOnlyStartLikeIt_Are()
        {
            using var container = Open();
            Assert.That(container.GetAllFiles().Keys, Is.EquivalentTo(new[] { @"db\units.tsv", ".gitignore", @".github\workflow.yml" }));
        }

        [Test]
        public void TheGitFolder_IsNotPacked()
        {
            var output = Path.Combine(_tempDir, "..", Path.GetFileName(_tempDir) + ".pack");
            try
            {
                using (var container = Open())
                    container.SaveToDisk(output, false, GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3));

                using var stream = File.OpenRead(output);
                using var reader = new BinaryReader(stream);
                var pack = PackFileSerializerLoader.Load(output, stream.Length, reader, new CustomPackDuplicateFileResolver());
                Assert.That(pack.FindFile(@"db\units.tsv"), Is.Not.Null);
                Assert.That(pack.GetAllFiles().Keys.Where(x => x.StartsWith(@".git\")), Is.Empty);
            }
            finally
            {
                File.Delete(output);
            }
        }

        [Test]
        public void TheDefault_IsWrittenIntoTheProjectFile_SoItCanBeEdited()
        {
            File.WriteAllText(Path.Combine(_tempDir, "aeproject.json"), """{ "SaveLocationPath": "c:\\out\\mod.pack" }""");
            using (Open()) { }

            var saved = PackFileSettings.Load(Path.Combine(_tempDir, "aeproject.json"), new FileSystemAccess())!;
            Assert.That(saved.ExcludedPaths, Is.EqualTo(new[] { ".git" }));
        }

        [Test]
        public void PathsListedInTheProjectFile_AreNotLoaded_AndTheListReplacesTheDefault()
        {
            Write(@"work\scratch.blend");
            Write(@"db\notes\todo.txt");
            File.WriteAllText(Path.Combine(_tempDir, "aeproject.json"), """{ "ExcludedPaths": [ "work", "db/notes" ] }""");

            using var container = Open();
            var files = container.GetAllFiles().Keys;
            Assert.That(files, Has.None.StartsWith(@"work\"));
            Assert.That(files, Has.None.StartsWith(@"db\notes\"));
            Assert.That(files, Does.Contain(@"db\units.tsv"));
            Assert.That(files, Does.Contain(@".git\head"), "the list replaces the default");
        }

        [Test]
        public void AnEmptyOrNullList_LoadsEverything()
        {
            File.WriteAllText(Path.Combine(_tempDir, "aeproject.json"), """{ "ExcludedPaths": null }""");
            using var container = Open();
            Assert.That(container.GetAllFiles().Keys, Does.Contain(@".git\head"));
        }

        [Test]
        public void AFileGitWritesWhileTheProjectIsOpen_IsNotAdded()
        {
            using var container = Open();
            Write(@".git\objects\12\3456");
            Write("new.txt");

            _watcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.Combine(_tempDir, @".git\objects\12"), "3456"));
            _watcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "new.txt"));
            container.ProcessPendingEvents(null);

            Assert.That(container.GetAllFiles().Keys, Does.Contain("new.txt"));
            Assert.That(container.GetAllFiles().Keys, Has.None.StartsWith(@".git\"));
        }
    }
}
