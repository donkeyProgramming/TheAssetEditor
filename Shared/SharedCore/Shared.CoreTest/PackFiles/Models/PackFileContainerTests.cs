using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.CoreTest.PackFiles.Models
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

            Assert.That(container.FileList.Count, Is.EqualTo(4));
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

            Assert.That(container.FileList.Count, Is.EqualTo(1));
            Assert.That(container.FileList.ContainsKey("rootfile.txt"), Is.True);
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

            Assert.That(container.FileList.Count, Is.EqualTo(3));
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

            Assert.That(container.FileList.Count, Is.EqualTo(1));
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

            Assert.That(container.FileList.Count, Is.EqualTo(1));
            Assert.That(container.FileList.First().Key.Any(char.IsWhiteSpace), Is.False);
            Assert.That(container.FileList.First().Value.Name.Any(char.IsWhiteSpace), Is.False);
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
            var file = container.FileList.Values.First();

            container.DeleteFile(file);

            Assert.That(container.FileList.Count, Is.EqualTo(3));
            Assert.That(container.FileList.Values, Does.Not.Contain(file));
        }

        [Test]
        public void DeleteFile_ReturnsDeletedFile()
        {
            var container = CreateContainerWithFiles();
            var file = container.FileList.Values.First();

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
    }

    internal class PackFileContainer_DeleteFolder
    {
        [Test]
        public void DeleteFolder_RemovesFilesInFolder()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("directory_0");

            Assert.That(container.FileList.Count, Is.EqualTo(2));
        }

        [Test]
        public void DeleteFolder_MissingFolder_NoEffect()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("nonexistent");

            Assert.That(container.FileList.Count, Is.EqualTo(8));
        }

        [Test]
        public void DeleteFolder_WithSubFolder()
        {
            var container = CreateTestContainer();

            container.DeleteFolder("directory_0\\subfolder");

            Assert.That(container.FileList.Count, Is.EqualTo(4));
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
            Assert.That(container.FileList.Count, Is.EqualTo(8));
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

            Assert.That(container.FileList.Count, Is.EqualTo(1));
            Assert.That(container.FileList.ContainsKey("NewDir\\file0.txt"), Is.True);
            Assert.That(container.FileList.ContainsKey("olddir\\file0.txt"), Is.False);
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
            Assert.That(container.FileList.Count, Is.EqualTo(2));
            Assert.That(container.FileList.Keys.All(k => k.StartsWith("NewName")), Is.True);
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
            Assert.That(container.FileList.ContainsKey("parent\\NewChild\\file0.txt"), Is.True);
            Assert.That(container.FileList.ContainsKey("parent\\NewChild\\sub\\file1.txt"), Is.True);
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
            Assert.That(container.FileList.Count, Is.EqualTo(1));
            Assert.That(container.FileList.ContainsKey("dir\\new.txt"), Is.True);
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
}
