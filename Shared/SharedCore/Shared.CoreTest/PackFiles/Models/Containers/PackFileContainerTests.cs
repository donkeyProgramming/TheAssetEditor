using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal class PackFileContainer_AddFiles
    {
        [Test]
        public void AddFiles_MultipleFiles()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),
                new("Directory_1", new PackFile("file0.txt", null)),
                new("", new PackFile("rootFile.txt", null))
            };

            var result = container.AddFiles(newFiles);

            Assert.That(container.GetFileCount(), Is.EqualTo(4));
            Assert.That(result.Count, Is.EqualTo(4));
        }

        [Test]
        public void AddFiles_AddToRoot()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("", new PackFile("rootFile.txt", null))
            };

            container.AddFiles(newFiles);

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.ContainsFile("rootfile.txt"), Is.True);
        }

        [Test]
        public void AddFiles_AddToFolder()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),
                new("Directory_1", new PackFile("file0.txt", null)),
            };

            container.AddFiles(newFiles);

            Assert.That(container.GetFileCount(), Is.EqualTo(3));
        }

        [Test]
        public void AddFiles_FileNameConflict_Override()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
            };

            container.AddFiles(newFiles);
            container.AddFiles(newFiles);

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
        }

        [Test]
        public void AddFiles_WhiteSpaceInName()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0  ", new PackFile("file0.txt   ", null)),
            };

            container.AddFiles(newFiles);

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.GetAllFiles().First().Key.Any(char.IsWhiteSpace), Is.False);
            Assert.That(container.GetAllFiles().First().Value.Name.Any(char.IsWhiteSpace), Is.False);
        }

        [Test]
        public void AddFiles_EmptyFileName_Throws()
        {
            var container = new PackFileContainer("Test");
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("", null)),
            };

            Assert.Throws<Exception>(() => container.AddFiles(newFiles));
        }

        [Test]
        public void AddFiles_ReturnsAddedFiles()
        {
            var container = new PackFileContainer("Test");
            var file0 = new PackFile("file0.txt", null);
            var file1 = new PackFile("file1.txt", null);
            var newFiles = new List<NewPackFileEntry>
            {
                new("Dir", file0),
                new("Dir", file1),
            };

            var result = container.AddFiles(newFiles);

            Assert.That(result, Contains.Item(file0));
            Assert.That(result, Contains.Item(file1));
        }
    }

    internal class PackFileContainer_DeleteFile
    {
        [Test]
        public void DeleteFile_RemovesFile()
        {
            var container = CreateContainerWithFiles();
            var file = container.GetAllFiles().Values.First();

            container.DeleteFile(file);

            Assert.That(container.GetFileCount(), Is.EqualTo(3));
            Assert.That(container.GetAllFiles().Values, Does.Not.Contain(file));
        }

        [Test]
        public void DeleteFile_ReturnsDeletedFile()
        {
            var container = CreateContainerWithFiles();
            var file = container.GetAllFiles().Values.First();

            var result = container.DeleteFile(file);

            Assert.That(result, Is.EqualTo(file));
        }

        static PackFileContainer CreateContainerWithFiles()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("Dir_0", new PackFile("file0.txt", null)),
                new("Dir_0", new PackFile("file1.txt", null)),
                new("Dir_1", new PackFile("file0.txt", null)),
                new("", new PackFile("rootFile.txt", null)),
            });
            return container;
        }

        [Test]
        public void DeleteFile_FileNotInContainer_ReturnsNull()
        {
            var container = CreateContainerWithFiles();
            var orphanFile = new PackFile("orphan.txt", null);

            var result = container.DeleteFile(orphanFile);

            Assert.That(result, Is.Null);
            Assert.That(container.GetFileCount(), Is.EqualTo(4));
        }
    }

    internal class PackFileContainer_DeleteFolder
    {
        [Test]
        public void DeleteFolder_RemovesFilesInFolder()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("directory_0");

            Assert.That(container.GetFileCount(), Is.EqualTo(2));
        }

        [Test]
        public void DeleteFolder_MissingFolder_NoEffect()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("nonexistent");

            Assert.That(container.GetFileCount(), Is.EqualTo(8));
        }

        [Test]
        public void DeleteFolder_WithSubFolder()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("directory_0\\subfolder");

            Assert.That(container.GetFileCount(), Is.EqualTo(4));
        }

        [Test]
        public void DeleteFolder_SimilarPrefixFolder_DoesNotDeleteWrongFiles()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("dir", new PackFile("file0.txt", null)),
                new("directory", new PackFile("file1.txt", null)),
            });

            container.DeleteFolder("dir");

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.FindFile("directory\\file1.txt"), Is.Not.Null);
        }

        static PackFileContainer CreateTestContainer()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),
                new("Directory_0\\subfolder", new PackFile("subfile0.txt", null)),
                new("Directory_0\\subfolder", new PackFile("subfile1.txt", null)),
                new("Directory_0\\subfolder\\child", new PackFile("childFile0.txt", null)),
                new("Directory_0\\subfolder\\child", new PackFile("childFile1.txt", null)),
                new("Directory_1", new PackFile("file0.txt", null)),
                new("", new PackFile("rootFile.txt", null))
            });
            Assert.That(container.GetFileCount(), Is.EqualTo(8));
            return container;
        }
    }

    internal class PackFileContainer_FindFile
    {
        [Test]
        public void FindFile_ExistingFile()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var result = container.FindFile("Dir\\file0.txt");

            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_CaseInsensitive()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var result = container.FindFile("DIR\\FILE0.TXT");

            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_ForwardSlash()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var result = container.FindFile("Dir/file0.txt");

            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_NotFound_ReturnsNull()
        {
            var container = new PackFileContainer("Test");

            var result = container.FindFile("nonexistent.txt");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindFile_TrimWhitespace()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var result = container.FindFile("  Dir\\file0.txt  ");

            Assert.That(result, Is.EqualTo(file));
        }
    }

    internal class PackFileContainer_GetFullPath
    {
        [Test]
        public void GetFullPath_ByReference()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var result = container.GetFullPath(file);

            Assert.That(result, Is.EqualTo("dir\\file0.txt"));
        }

        [Test]
        public void GetFullPath_ByName()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var otherRef = new PackFile("file0.txt", null);
            var result = container.GetFullPath(otherRef);

            Assert.That(result, Is.EqualTo("dir\\file0.txt"));
        }

        [Test]
        public void GetFullPath_NotFound_ReturnsNull()
        {
            var container = new PackFileContainer("Test");

            var result = container.GetFullPath(new PackFile("nonexistent.txt", null));

            Assert.That(result, Is.Null);
        }
    }

    internal class PackFileContainer_MoveFile
    {
        [Test]
        public void MoveFile_MovesToNewFolder()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("OldDir", file) });

            container.MoveFile(file, "NewDir");

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.ContainsFile("newdir\\file0.txt"), Is.True);
            Assert.That(container.ContainsFile("olddir\\file0.txt"), Is.False);
        }

        [Test]
        public void MoveFile_NormalizesPathCasing()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file0.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("OldDir", file) });

            container.MoveFile(file, "NewDir");

            var found = container.FindFile("NewDir\\file0.txt");
            Assert.That(found, Is.Not.Null);
        }

    }

    internal class PackFileContainer_RenameDirectory
    {
        [Test]
        public void RenameDirectory_TopLevel()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("OldName", new PackFile("file0.txt", null)),
                new("OldName", new PackFile("file1.txt", null)),
            });

            var newNodePath = container.RenameDirectory("oldname", "NewName");

            Assert.That(newNodePath, Is.EqualTo("NewName"));
            Assert.That(container.GetFileCount(), Is.EqualTo(2));
            Assert.That(container.GetAllFiles().Keys.All(k => k.StartsWith("newname", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public void RenameDirectory_Nested()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("Parent\\OldChild", new PackFile("file0.txt", null)),
                new("Parent\\OldChild\\Sub", new PackFile("file1.txt", null)),
            });

            var newNodePath = container.RenameDirectory("parent\\oldchild", "NewChild");

            Assert.That(newNodePath, Is.EqualTo("parent\\NewChild"));
            Assert.That(container.ContainsFile("parent\\newchild\\file0.txt"), Is.True);
            Assert.That(container.ContainsFile("parent\\newchild\\sub\\file1.txt"), Is.True);
        }

        [Test]
        public void RenameDirectory_ReturnsNewNodePath()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("Parent\\Child", new PackFile("file0.txt", null)),
            });

            var result = container.RenameDirectory("parent\\child", "RenamedChild");

            Assert.That(result, Is.EqualTo("parent\\RenamedChild"));
        }

        [Test]
        public void RenameDirectory_NormalizesPathCasing()
        {
            var container = new PackFileContainer("Test");
            container.AddFiles(new List<NewPackFileEntry>
            {
                new("OldDir", new PackFile("file.txt", null)),
            });

            container.RenameDirectory("olddir", "NewDir");

            var found = container.FindFile("NewDir\\file.txt");
            Assert.That(found, Is.Not.Null);
        }
    }

    internal class PackFileContainer_RenameFile
    {
        [Test]
        public void RenameFile_UpdatesNameAndPath()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("old.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            container.RenameFile(file, "new.txt");

            Assert.That(file.Name, Is.EqualTo("new.txt"));
            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.ContainsFile("dir\\new.txt"), Is.True);
        }

        [Test]
        public void RenameFile_RootFile_ProducesValidPath()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("old.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("", file) });

            container.RenameFile(file, "new.txt");

            Assert.That(container.ContainsFile("new.txt"), Is.True);
            Assert.That(container.GetAllFiles().Keys.Any(k => k.StartsWith("\\")), Is.False);
        }

        [Test]
        public void RenameFile_NormalizesPathCasing()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("old.txt", null);
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            container.RenameFile(file, "New.txt");

            var found = container.FindFile("Dir\\New.txt");
            Assert.That(found, Is.Not.Null);
        }

    }

    internal class PackFileContainer_SaveFileData
    {
        [Test]
        public void SaveFileData_UpdatesDataSource()
        {
            var container = new PackFileContainer("Test");
            var file = new PackFile("file.txt", new MemorySource([1, 2, 3]));
            container.AddFiles(new List<NewPackFileEntry> { new("Dir", file) });

            var newData = new byte[] { 4, 5, 6, 7 };
            container.SaveFileData(file, newData);

            Assert.That(file.DataSource.ReadData(), Is.EqualTo(newData));
        }

    }

    internal class PackFileContainer_SearchFiles
    {
        private PackFileContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new PackFileContainer("Test");
            _container.AddOrUpdateFile("models\\unit.rigid_model_v2", new PackFile("unit.rigid_model_v2", null));
            _container.AddOrUpdateFile("models\\vehicle.rigid_model_v2", new PackFile("vehicle.rigid_model_v2", null));
            _container.AddOrUpdateFile("models\\textures\\diffuse.dds", new PackFile("diffuse.dds", null));
            _container.AddOrUpdateFile("models\\textures\\normal.dds", new PackFile("normal.dds", null));
            _container.AddOrUpdateFile("audio\\battle_sound.wem", new PackFile("battle_sound.wem", null));
            _container.AddOrUpdateFile("audio\\music.wem", new PackFile("music.wem", null));
            _container.AddOrUpdateFile("scripts\\campaign_script.lua", new PackFile("campaign_script.lua", null));
        }

        [Test]
        public void NullFilters_ReturnsAllFiles()
        {
            var results = _container.SearchFiles(null, null);
            Assert.That(results.Count, Is.EqualTo(7));
        }

        [Test]
        public void TextFilter_MatchesFileName()
        {
            var results = _container.SearchFiles("unit", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.rigid_model_v2"));
        }

        [Test]
        public void TextFilter_IsCaseInsensitive()
        {
            var results = _container.SearchFiles("UNIT", null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].File.Name, Is.EqualTo("unit.rigid_model_v2"));
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
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(r => r.File.Name.EndsWith(".wem")), Is.True);
        }

        [Test]
        public void ExtensionFilter_MultipleExtensions()
        {
            var results = _container.SearchFiles(null, [".wem", ".lua"]);
            Assert.That(results.Count, Is.EqualTo(3));
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
        public void EmptyTextFilter_TreatedAsNull()
        {
            var results = _container.SearchFiles("   ", null);
            Assert.That(results.Count, Is.EqualTo(7));
        }

        [Test]
        public void EmptyExtensionList_TreatedAsNoFilter()
        {
            var results = _container.SearchFiles(null, []);
            Assert.That(results.Count, Is.EqualTo(7));
        }
    }

    internal class PackFileContainer_GetDirectoryContent
    {
        [Test]
        public void Root_ReturnsOnlyDirectFiles_Sorted()
        {
            var container = CreateContainer();

            var entries = container.GetDirectoryContent("");
            var paths = entries.Select(x => x.Path).ToList();

            Assert.That(paths, Is.EqualTo(new[] { "root_a.txt", "root_b.txt" }));
        }

        [Test]
        public void SubFolder_ReturnsOnlyDirectFiles()
        {
            var container = CreateContainer();

            var entries = container.GetDirectoryContent("texture");

            Assert.That(entries.Select(x => x.Path), Is.EqualTo(new[] { "texture\\texture_file.dds" }));
        }

        [Test]
        public void GetSubDirectories_ReturnsImmediateSubfolders()
        {
            var container = CreateContainer();

            var subDirectories = container.GetSubDirectories("texture");

            Assert.That(subDirectories, Is.EqualTo(new[] { "mesha", "meshb" }));
        }

        [Test]
        public void Utility_ComposesFilesAndSubfolders()
        {
            var container = CreateContainer();
            var split = PackFileServiceUtility.SplitDirectoryEntries(container, "texture");

            Assert.That(split.Files.Select(x => x.FileName), Is.EqualTo(new[] { "texture_file.dds" }));
            Assert.That(split.SubFolders, Is.EqualTo(new[] { "mesha", "meshb" }));
        }

        private static PackFileContainer CreateContainer()
        {
            var container = new PackFileContainer("Test");
            var parent = new PackedFileSourceParent { FilePath = @"c:\game\p.pack" };

            container.AddOrUpdateFile(@"audio\b.wem", new PackFile("b.wem", new PackedFileSource(parent, 0, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"audio\a.wem", new PackFile("a.wem", new PackedFileSource(parent, 1, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\texture_file.dds", new PackFile("texture_file.dds", new PackedFileSource(parent, 2, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\mesha\filea", new PackFile("filea", new PackedFileSource(parent, 3, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"texture\meshb\filea", new PackFile("filea", new PackedFileSource(parent, 4, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"root_b.txt", new PackFile("root_b.txt", new PackedFileSource(parent, 5, 1, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile(@"root_a.txt", new PackFile("root_a.txt", new PackedFileSource(parent, 6, 1, false, false, CompressionFormat.None, 0)));

            return container;
        }
    }
}
