using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal abstract class CachedPackFileContainer_TestBase
    {
        protected string _tempDir = null!;
        protected string _dbFilePath = null!;
        protected CachedPackFileContainer _container = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "CachedContainerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _dbFilePath = Path.Combine(_tempDir, "test.db");

            // Master test dataset used by all fixtures in this file.
            var sourceContainer = new PackFileContainer("TestCache")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };
            sourceContainer.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            sourceContainer.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt",
                new PackedFileSource(parent, 100, 200, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("other\\data.bin", new PackFile("data.bin",
                new PackedFileSource(parent, 300, 400, false, true, CompressionFormat.Lz4, 800)));
            sourceContainer.AddOrUpdateFile("audio\\sound.wem", new PackFile("sound.wem",
                new PackedFileSource(parent, 700, 500, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("root_file.txt", new PackFile("root_file.txt",
                new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\unit.model", new PackFile("unit.model",
                new PackedFileSource(parent, 10, 20, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\vehicle.model", new PackFile("vehicle.model",
                new PackedFileSource(parent, 30, 40, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\diffuse.dds", new PackFile("diffuse.dds",
                new PackedFileSource(parent, 70, 50, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\normal.dds", new PackFile("normal.dds",
                new PackedFileSource(parent, 120, 60, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("models\\textures\\specular\\gloss.dds", new PackFile("gloss.dds",
                new PackedFileSource(parent, 180, 30, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\music.wem", new PackFile("music.wem",
                new PackedFileSource(parent, 210, 100, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("audio\\battle_sound.wem", new PackFile("battle_sound.wem",
                new PackedFileSource(parent, 400, 300, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("scripts\\campaign_script.lua", new PackFile("campaign_script.lua",
                new PackedFileSource(parent, 850, 80, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("folder_a\\shared.txt", new PackFile("shared.txt",
                new PackedFileSource(parent, 900, 10, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("folder_b\\shared.txt", new PackFile("shared.txt",
                new PackedFileSource(parent, 910, 20, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("compressed\\data.bin", new PackFile("data.bin",
                new PackedFileSource(parent, 1000, 500, true, true, CompressionFormat.Lz4, 2000)));

            var dbOptions = PackFileContainerCacheHelper.CreateDbOptions(_dbFilePath);
            PackFileContainerCacheHelper.SaveCache("test_fp", sourceContainer, dbOptions);
            _container = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "test_fp")!;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
    }

    internal class CachedPackFileContainer_ReadOnly : CachedPackFileContainer_TestBase
    {

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
            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("new.txt", null))
            };
            Assert.Throws<InvalidOperationException>(() => _container.AddFiles(newFiles));
        }

        [Test]
        public void DeleteFile_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
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
            var file = _container.FindFile("folder\\file.txt")!;
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
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.RenameFile(file, "renamed.txt"));
        }

        [Test]
        public void SaveFileData_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.SaveFileData(file, [1, 2, 3]));
        }

        [Test]
        public void SaveToDisk_Throws()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => _container.SaveToDisk("path", false, gameInfo));
        }

        [Test]
        public void AddOrUpdateFile_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _container.AddOrUpdateFile("test\\new.txt", new PackFile("new.txt", null)));
        }
    }

    internal class CachedPackFileContainer_GetDirectoryContent : CachedPackFileContainer_TestBase
    {
        [Test]
        public void Root_ReturnsRootFilesAndTopFolders()
        {
            var content = PackFileServiceUtility.SplitDirectoryEntries(_container, "");

            Assert.That(content.Files.Any(f => f.FileName == "root_file.txt"), Is.True);
            Assert.That(content.SubFolders, Does.Contain("models"));
            Assert.That(content.SubFolders, Does.Contain("audio"));
            // Should NOT contain nested folders at root level
            Assert.That(content.SubFolders, Does.Not.Contain("textures"));
        }

        [Test]
        public void Root_DoesNotIncludeFilesFromSubfolders()
        {
            var entries = _container.GetDirectoryContent("");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].File.Name, Is.EqualTo("root_file.txt"));
        }

        [Test]
        public void Subfolder_ReturnsDirectFilesAndImmediateSubfolders()
        {
            var entries = _container.GetDirectoryContent("models");
            var subFolders = _container.GetSubDirectories("models");

            Assert.That(entries.Any(f => f.File.Name == "unit.model"), Is.True);
            Assert.That(entries.Any(f => f.File.Name == "vehicle.model"), Is.True);
            Assert.That(subFolders, Does.Contain("textures"));
            Assert.That(entries.Any(f => f.File.Name == "diffuse.dds"), Is.False);
        }

        [Test]
        public void NestedSubfolder_ReturnsCorrectContent()
        {
            var entries = _container.GetDirectoryContent("models\\textures");
            var subFolders = _container.GetSubDirectories("models\\textures");

            Assert.That(entries.Any(f => f.File.Name == "diffuse.dds"), Is.True);
            Assert.That(entries.Any(f => f.File.Name == "normal.dds"), Is.True);
            Assert.That(subFolders, Does.Contain("specular"));
            Assert.That(entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void DeepNestedFolder_ReturnsOnlyItsFiles()
        {
            var entries = _container.GetDirectoryContent("models\\textures\\specular");
            var subFolders = _container.GetSubDirectories("models\\textures\\specular");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].File.Name, Is.EqualTo("gloss.dds"));
            Assert.That(subFolders, Is.Empty);
        }

        [Test]
        public void NonexistentFolder_ReturnsEmpty()
        {
            var entries = _container.GetDirectoryContent("nonexistent\\path");
            var subFolders = _container.GetSubDirectories("nonexistent\\path");

            Assert.That(entries, Is.Empty);
            Assert.That(subFolders, Is.Empty);
        }

        [Test]
        public void SubfoldersAreSorted()
        {
            var subFolders = _container.GetSubDirectories("");

            var sorted = subFolders.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
            Assert.That(subFolders, Is.EqualTo(sorted));
        }

        [Test]
        public void FilesAreSorted()
        {
            var entries = _container.GetDirectoryContent("models");

            var sorted = entries.OrderBy(x => x.File.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            Assert.That(entries.Select(f => f.File.Name), Is.EqualTo(sorted.Select(f => f.File.Name)));
        }

        [Test]
        public void Files_HaveCorrectDataSource()
        {
            var entries = _container.GetDirectoryContent("models");
            var unitFile = entries.First(f => f.File.Name == "unit.model");
            var source = unitFile.File.DataSource as PackedFileSource;

            Assert.That(source, Is.Not.Null);
            Assert.That(source.Offset, Is.EqualTo(10));
            Assert.That(source.Size, Is.EqualTo(20));
            Assert.That(source.Parent.FilePath, Is.EqualTo(@"c:\game\data\pack1.pack"));
        }
    }

    internal class CachedPackFileContainer_EdgeCases : CachedPackFileContainer_TestBase
    {
        [Test]
        public void FindAllWithExtention_IsCaseInsensitive()
        {
            var upper = _container.FindAllWithExtention(".WEM");
            var lower = _container.FindAllWithExtention(".wem");
            Assert.That(upper.Count, Is.EqualTo(3));
            Assert.That(lower.Count, Is.EqualTo(3));
        }

        [Test]
        public void ContainsFile_NormalizesForwardSlashes()
        {
            Assert.That(_container.ContainsFile("folder/file.txt"), Is.True);
            Assert.That(_container.ContainsFile("FOLDER/FILE.TXT"), Is.True);
            Assert.That(_container.ContainsFile(" folder\\file.txt "), Is.True);
        }

        [Test]
        public void GetAllFiles_PreservesCompressionMetadata()
        {
            var all = _container.GetAllFiles();
            var source = (PackedFileSource)all["compressed\\data.bin"].DataSource;

            Assert.That(source.IsEncrypted, Is.True);
            Assert.That(source.IsCompressed, Is.True);
            Assert.That(source.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(source.UncompressedSize, Is.EqualTo(2000));
            Assert.That(source.Offset, Is.EqualTo(1000));
            Assert.That(source.Size, Is.EqualTo(500));
        }

        [Test]
        public void GetFullPath_WithDuplicateFileNames_ReturnsFirstMatch()
        {
            // GetFullPath matches by FileName - with duplicates it returns the first DB row
            var file = _container.FindFile("folder_a\\shared.txt")!;
            var path = _container.GetFullPath(file);

            // Should return a valid path (either folder_a or folder_b)
            Assert.That(path, Does.Contain("shared.txt"));
            Assert.That(path, Does.Contain("\\"));
        }

        [Test]
        public void GetDirectoryContent_UnknownFolder_ReturnsEmpty()
        {
            Assert.That(_container.GetDirectoryContent("missing\\folder"), Is.Empty);
            Assert.That(_container.GetSubDirectories("missing\\folder"), Is.Empty);
        }
    }

    internal class CachedPackFileContainer_SearchFiles : CachedPackFileContainer_TestBase
    {
        [Test]
        public void NullFilters_ReturnsAllFiles()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(15));
        }

        [Test]
        public void TextFilter_MatchesFileName()
        {
            var results = _container.SearchFiles("unit", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.model"));
        }

        [Test]
        public void TextFilter_IsCaseInsensitive()
        {
            var results = _container.SearchFiles("UNIT", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.model"));
        }

        [Test]
        public void TextFilter_PartialMatch()
        {
            var results = _container.SearchFiles("battle", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("battle_sound.wem"));
        }

        [Test]
        public void TextFilter_NoMatch_ReturnsEmpty()
        {
            var results = _container.SearchFiles("nonexistent", null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ExtensionFilter_SingleExtension()
        {
            var results = _container.SearchFiles(null, [".wem"]);
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.All(r => r.File.Name.EndsWith(".wem")), Is.True);
        }

        [Test]
        public void ExtensionFilter_MultipleExtensions()
        {
            var results = _container.SearchFiles(null, [".wem", ".lua"]);
            Assert.That(results.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExtensionFilter_NoMatch_ReturnsEmpty()
        {
            var results = _container.SearchFiles(null, [".xyz"]);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void CombinedFilters_TextAndExtension()
        {
            var results = _container.SearchFiles("battle", [".wem"]);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("battle_sound.wem"));
        }

        [Test]
        public void CombinedFilters_TextMatchesButExtensionDoesNot_ReturnsEmpty()
        {
            var results = _container.SearchFiles("unit", [".wem"]);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ResultsAreSortedByPath()
        {
            var results = _container.SearchFiles(null, null);
            var paths = results.Select(r => r.Path).ToList();
            var sorted = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(paths, Is.EqualTo(sorted));
        }

        [Test]
        public void ResultsContainCorrectPaths()
        {
            var results = _container.SearchFiles("diffuse", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Path, Is.EqualTo("models\\textures\\diffuse.dds"));
        }

        [Test]
        public void EmptyExtensionList_TreatedAsNoFilter()
        {
            var results = _container.SearchFiles(null, []);
            Assert.That(results.Count, Is.EqualTo(15));
        }
    }

    [TestFixture]
    internal class CachedPackFileContainer_GetDirectoryContent_PathRows : CachedPackFileContainer_TestBase
    {
        [Test]
        public void Root_IncludesOnlyRootFiles()
        {
            var rows = _container.GetDirectoryContent("");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Contain("root_file.txt"));
        }

        [Test]
        public void Root_ExcludesDeepDescendantFolders()
        {
            var rows = _container.GetDirectoryContent("");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Not.Contain(@"texture\mesha\filea"));
            Assert.That(paths, Does.Not.Contain(@"texture\meshb\filea"));
        }

        [Test]
        public void Root_ReturnsExpectedRowCount()
        {
            // root contains only its direct files
            var rows = _container.GetDirectoryContent("");
            Assert.That(rows.Count, Is.EqualTo(1));
        }

        [Test]
        public void Subfolder_IncludesOnlyDirectFiles()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths, Does.Contain(@"models\textures\diffuse.dds"));
            Assert.That(paths, Does.Contain(@"models\textures\normal.dds"));
            Assert.That(paths.Count, Is.EqualTo(2));
        }

        [Test]
        public void Subfolder_ExcludesParentAndSiblingRows()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            var paths = rows.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.That(paths.Any(p => p.StartsWith(@"audio\", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(paths.Any(p => !p.StartsWith(@"models\textures\", StringComparison.OrdinalIgnoreCase)), Is.False);
        }

        [Test]
        public void Subfolder_ReturnsExpectedRowCount()
        {
            var rows = _container.GetDirectoryContent(@"models\textures");
            Assert.That(rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void LeafFolder_ReturnsOnlyDirectFiles()
        {
            var rows = _container.GetDirectoryContent(@"audio");
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows.All(r => r.Path.StartsWith(@"audio\", StringComparison.OrdinalIgnoreCase)), Is.True);
        }
    }
}
