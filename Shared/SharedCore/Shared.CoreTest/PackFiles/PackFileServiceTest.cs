using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles
{
    internal class PackFileServiceTest
    {
        private static PackFileService CreateService(Mock<IGlobalEventHub>? eventHub = null)
        {
            eventHub ??= new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            return pfs;
        }

        private static PackFileService CreateServiceWithCaPack(Mock<IGlobalEventHub>? eventHub = null)
        {
            var pfs = CreateService(eventHub);
            var ca = new PackFileContainer("CaPack") { IsCaPackFile = true, SystemFilePath = "ca_path" };
            pfs.AddContainer(ca);
            return pfs;
        }

        [Test]
        public void AddContainer_CaPackFile()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = true;

            pfs.AddContainer(container);

            var containers = pfs.GetAllPackfileContainers();
            Assert.That(containers.First(), Is.EqualTo(container));
            Assert.That(pfs.GetEditablePack(), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Once);
        }

        [Test]
        public void AddContainer_CaPackFileNotSet()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var dialogProvider = new Mock<ISimpleMessageBox>();
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = dialogProvider.Object;
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = false;

            pfs.AddContainer(container);

            var containers = pfs.GetAllPackfileContainers();
            Assert.That(containers.Count, Is.EqualTo(0));
            Assert.That(pfs.GetEditablePack(), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Never);
            dialogProvider.Verify(m => m.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AddContainer_CaPackFileSet()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            var caContainer = new PackFileContainer("MyTest");
            caContainer.SystemFilePath = "SystemPath";
            caContainer.IsCaPackFile = true;

            var customContainer = new PackFileContainer("MyTest");
            customContainer.SystemFilePath = "SystemPath2";
            customContainer.IsCaPackFile = false;

            pfs.AddContainer(caContainer);
            pfs.AddContainer(customContainer, true);

            var containers = pfs.GetAllPackfileContainers();
            Assert.That(containers.Count, Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(customContainer));
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(caContainer)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(customContainer)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(customContainer)), Times.Once);
        }

        [Test]
        public void AddContainers_Duplicate()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var dialogProvider = new Mock<ISimpleMessageBox>();
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = dialogProvider.Object;
            var caContainer = new PackFileContainer("MyTest");
            caContainer.SystemFilePath = "SystemPath";
            caContainer.IsCaPackFile = true;

            var customContainer = new PackFileContainer("MyTest");
            customContainer.SystemFilePath = "SystemPath2";
            customContainer.IsCaPackFile = false;

            pfs.AddContainer(caContainer);
            pfs.AddContainer(customContainer, true);
            pfs.AddContainer(customContainer);

            var containers = pfs.GetAllPackfileContainers();
            Assert.That(containers.Count, Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(customContainer));
            dialogProvider.Verify(m => m.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void CreateNewPackFileContainer()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = true;

            pfs.AddContainer(container);
            var emptyPackFileContainer = pfs.CreateNewPackFileContainer("Custom", PackFileVersion.PFH5, PackFileCAType.MOD, true);

            var containers = pfs.GetAllPackfileContainers();
            Assert.That(containers.Count(), Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(emptyPackFileContainer));
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(emptyPackFileContainer)), Times.Once);
        }

        [Test]
        public void CreateNewPackFileContainer_EmptyName_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            Assert.Throws<Exception>(() => pfs.CreateNewPackFileContainer("", PackFileVersion.PFH5, PackFileCAType.MOD));
            Assert.Throws<Exception>(() => pfs.CreateNewPackFileContainer("  ", PackFileVersion.PFH5, PackFileCAType.MOD));
        }

        [Test]
        public void SaveFile_NoEditablePack_ThrowsDescriptiveException()
        {
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("Ca") { IsCaPackFile = true });
            var file = new PackFile("file.txt", new MemorySource([1, 2, 3]));

            var ex = Assert.Throws<Exception>(() => pfs.SaveFile(file, [4, 5, 6]));
            Assert.That(ex.Message, Does.Contain("No editable pack file is set"));
        }

        [Test]
        public void AddContainer_TwoContainersWithNullSystemFilePath_BothAdded()
        {
            var dialogProvider = new Mock<ISimpleMessageBox>();
            var pfs = new PackFileService(null);
            pfs.MessageBoxProvider = dialogProvider.Object;
            var ca = new PackFileContainer("Ca") { IsCaPackFile = true };
            pfs.AddContainer(ca);

            var container1 = new PackFileContainer("Pack1") { SystemFilePath = null };
            var container2 = new PackFileContainer("Pack2") { SystemFilePath = null };

            pfs.AddContainer(container1);
            var result = pfs.AddContainer(container2);

            Assert.That(result, Is.Not.Null);
            Assert.That(pfs.GetAllPackfileContainers().Count, Is.EqualTo(3));
        }

        [Test]
        public void AddContainer_EnforceGameFilesDisabled_AllowsNonCaFirst()
        {
            var pfs = CreateService();
            pfs.EnforceGameFilesMustBeLoaded = false;

            var container = new PackFileContainer("Custom") { IsCaPackFile = false, SystemFilePath = "path" };
            var result = pfs.AddContainer(container);

            Assert.That(result, Is.Not.Null);
            Assert.That(pfs.GetAllPackfileContainers().Count, Is.EqualTo(1));
        }

        [Test]
        public void SetEditablePack_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();

            Assert.Throws<Exception>(() => pfs.SetEditablePack(caPack));
        }

        [Test]
        public void SetEditablePack_Null_ClearsSelection()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var custom = new PackFileContainer("Custom") { SystemFilePath = "custom_path" };
            pfs.AddContainer(custom, true);

            Assert.That(pfs.GetEditablePack(), Is.EqualTo(custom));
            pfs.SetEditablePack(null);
            Assert.That(pfs.GetEditablePack(), Is.Null);
        }

        [Test]
        public void UnloadPackContainer_RemovesContainer()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var custom = new PackFileContainer("Custom") { SystemFilePath = "custom_path" };
            pfs.AddContainer(custom, true);

            pfs.UnloadPackContainer(custom);

            Assert.That(pfs.GetAllPackfileContainers().Count, Is.EqualTo(1));
            Assert.That(pfs.GetEditablePack(), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerRemovedEvent>()), Times.Once);
        }

        [Test]
        public void UnloadPackContainer_NonEditablePack_DoesNotClearEditable()
        {
            var pfs = CreateServiceWithCaPack();
            var pack1 = new PackFileContainer("Pack1") { SystemFilePath = "path1" };
            var pack2 = new PackFileContainer("Pack2") { SystemFilePath = "path2" };
            pfs.AddContainer(pack1, true);
            pfs.AddContainer(pack2);

            pfs.UnloadPackContainer(pack2);

            Assert.That(pfs.GetEditablePack(), Is.EqualTo(pack1));
            Assert.That(pfs.GetAllPackfileContainers().Count, Is.EqualTo(2));
        }

        [Test]
        public void AddFilesToPack_AddsFiles()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            pfs.AddContainer(container, true);

            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("test.txt", new MemorySource([1, 2, 3])))
            };

            pfs.AddFilesToPack(container, newFiles);

            Assert.That(container.FindFile("folder\\test.txt"), Is.Not.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Once);
        }

        [Test]
        public void AddFilesToPack_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();
            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("test.txt", new MemorySource([1])))
            };

            Assert.Throws<Exception>(() => pfs.AddFilesToPack(caPack, newFiles));
        }

        [Test]
        public void CopyFileFromOtherPackFile_CopiesData()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);

            var source = new PackFileContainer("Source") { SystemFilePath = "source_path" };
            source.AddOrUpdateFile("data\\file.bin", new PackFile("file.bin", new MemorySource([10, 20, 30])));
            pfs.AddContainer(source);

            var target = new PackFileContainer("Target") { SystemFilePath = "target_path" };
            pfs.AddContainer(target, true);

            pfs.CopyFileFromOtherPackFile(source, "data\\file.bin", target);

            var copied = target.FindFile("data\\file.bin");
            Assert.That(copied, Is.Not.Null);
            Assert.That(copied.Name, Is.EqualTo("file.bin"));
            Assert.That(copied.DataSource.ReadData(), Is.EqualTo(new byte[] { 10, 20, 30 }));
        }

        [Test]
        public void CopyFileFromOtherPackFile_MissingFile_DoesNothing()
        {
            var pfs = CreateServiceWithCaPack();
            var source = new PackFileContainer("Source") { SystemFilePath = "source_path" };
            pfs.AddContainer(source);
            var target = new PackFileContainer("Target") { SystemFilePath = "target_path" };
            pfs.AddContainer(target, true);

            pfs.CopyFileFromOtherPackFile(source, "nonexistent\\file.bin", target);

            Assert.That(target.GetFileCount(), Is.EqualTo(0));
        }

        [Test]
        public void CopyFileFromOtherPackFile_TargetIsCaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();
            var source = new PackFileContainer("Source") { SystemFilePath = "source_path" };
            source.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(source);

            Assert.Throws<Exception>(() => pfs.CopyFileFromOtherPackFile(source, "file.txt", caPack));
        }

        [Test]
        public void DeleteFile_RemovesFile()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("folder\\test.txt", new PackFile("test.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("folder\\test.txt")!;
            pfs.DeleteFile(container, file);

            Assert.That(container.FindFile("folder\\test.txt"), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerFilesRemovedEvent>()), Times.Once);
        }

        [Test]
        public void DeleteFile_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();
            var file = new PackFile("test.txt", new MemorySource([1]));

            Assert.Throws<Exception>(() => pfs.DeleteFile(caPack, file));
        }

        [Test]
        public void DeleteFolder_RemovesFiles()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("folder\\a.txt", new PackFile("a.txt", new MemorySource([1])));
            container.AddOrUpdateFile("folder\\b.txt", new PackFile("b.txt", new MemorySource([2])));
            container.AddOrUpdateFile("other\\c.txt", new PackFile("c.txt", new MemorySource([3])));
            pfs.AddContainer(container, true);

            pfs.DeleteFolder(container, "folder");

            Assert.That(container.FindFile("folder\\a.txt"), Is.Null);
            Assert.That(container.FindFile("folder\\b.txt"), Is.Null);
            Assert.That(container.FindFile("other\\c.txt"), Is.Not.Null);
        }

        [Test]
        public void DeleteFolder_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();

            Assert.Throws<Exception>(() => pfs.DeleteFolder(caPack, "folder"));
        }

        [Test]
        public void MoveFile_ChangesPath()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("old\\test.txt", new PackFile("test.txt", new MemorySource([1, 2])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("old\\test.txt")!;
            pfs.MoveFile(container, file, "new");

            Assert.That(container.FindFile("old\\test.txt"), Is.Null);
            Assert.That(container.FindFile("new\\test.txt"), Is.Not.Null);
        }

        [Test]
        public void MoveFile_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();
            var file = new PackFile("test.txt", new MemorySource([1]));

            Assert.Throws<Exception>(() => pfs.MoveFile(caPack, file, "new"));
        }

        [Test]
        public void RenameFile_ChangesName()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("folder\\old.txt", new PackFile("old.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("folder\\old.txt")!;
            pfs.RenameFile(container, file, "new.txt");

            Assert.That(file.Name, Is.EqualTo("new.txt"));
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerFilesUpdatedEvent>()), Times.Once);
        }

        [Test]
        public void RenameFile_EmptyName_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("folder\\file.txt")!;
            Assert.Throws<Exception>(() => pfs.RenameFile(container, file, ""));
            Assert.Throws<Exception>(() => pfs.RenameFile(container, file, "  "));
        }

        [Test]
        public void RenameFile_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();
            var file = new PackFile("test.txt", new MemorySource([1]));

            Assert.Throws<Exception>(() => pfs.RenameFile(caPack, file, "new.txt"));
        }

        [Test]
        public void RenameDirectory_ChangesPath()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("oldname\\file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            pfs.RenameDirectory(container, "oldname", "newname");

            Assert.That(container.FindFile("oldname\\file.txt"), Is.Null);
            Assert.That(container.FindFile("newname\\file.txt"), Is.Not.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerFolderRenamedEvent>()), Times.Once);
        }

        [Test]
        public void RenameDirectory_EmptyName_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            pfs.AddContainer(container, true);

            Assert.Throws<Exception>(() => pfs.RenameDirectory(container, "folder", ""));
        }

        [Test]
        public void RenameDirectory_CaPack_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var caPack = pfs.GetAllPackfileContainers().First();

            Assert.Throws<Exception>(() => pfs.RenameDirectory(caPack, "folder", "new"));
        }

        [Test]
        public void FindFile_FindsAcrossContainers()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("data\\file.txt", new PackFile("file.txt", new MemorySource([1, 2])));
            pfs.AddContainer(container, true);

            var result = pfs.FindFile("data\\file.txt");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_ReturnsNullForMissing()
        {
            var pfs = CreateServiceWithCaPack();
            var result = pfs.FindFile("nonexistent\\file.txt");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindFile_SpecificContainer_OnlySearchesThat()
        {
            var pfs = CreateServiceWithCaPack();
            var container1 = new PackFileContainer("Pack1") { SystemFilePath = "path1" };
            container1.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container1);

            var container2 = new PackFileContainer("Pack2") { SystemFilePath = "path2" };
            pfs.AddContainer(container2);

            var found = pfs.FindFile("file.txt", container2);
            Assert.That(found, Is.Null);

            var foundInCorrect = pfs.FindFile("file.txt", container1);
            Assert.That(foundInCorrect, Is.Not.Null);
        }

        [Test]
        public void FindFile_LaterContainerTakesPriority()
        {
            var pfs = CreateServiceWithCaPack();
            var container1 = new PackFileContainer("Pack1") { SystemFilePath = "path1" };
            container1.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container1);

            var container2 = new PackFileContainer("Pack2") { SystemFilePath = "path2" };
            container2.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([2])));
            pfs.AddContainer(container2);

            var result = pfs.FindFile("file.txt");
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 2 }));
        }

        [Test]
        public void FindAllWithExtention_SearchesAllContainers()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("a.txt", new PackFile("a.txt", new MemorySource([1])));
            container.AddOrUpdateFile("b.txt", new PackFile("b.txt", new MemorySource([2])));
            container.AddOrUpdateFile("c.bin", new PackFile("c.bin", new MemorySource([3])));
            pfs.AddContainer(container, true);

            var results = pfs.FindAllWithExtention(".txt");
            Assert.That(results.Count, Is.EqualTo(2));
        }

        [Test]
        public void FindAllWithExtention_SpecificContainer()
        {
            var pfs = CreateServiceWithCaPack();
            var container1 = new PackFileContainer("Pack1") { SystemFilePath = "path1" };
            container1.AddOrUpdateFile("a.txt", new PackFile("a.txt", new MemorySource([1])));
            pfs.AddContainer(container1);

            var container2 = new PackFileContainer("Pack2") { SystemFilePath = "path2" };
            container2.AddOrUpdateFile("b.txt", new PackFile("b.txt", new MemorySource([2])));
            pfs.AddContainer(container2);

            var results = pfs.FindAllWithExtention(".txt", container1);
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetFullPath_ReturnsPath()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("folder\\file.txt")!;
            var path = pfs.GetFullPath(file);
            Assert.That(path, Is.EqualTo("folder\\file.txt"));
        }

        [Test]
        public void GetFullPath_UnknownFile_Throws()
        {
            var pfs = CreateServiceWithCaPack();
            var unknownFile = new PackFile("unknown.txt", new MemorySource([1]));

            Assert.Throws<Exception>(() => pfs.GetFullPath(unknownFile));
        }

        [Test]
        public void GetPackFileContainer_ReturnsCorrectContainer()
        {
            var pfs = CreateServiceWithCaPack();
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([1])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("file.txt")!;
            var result = pfs.GetPackFileContainer(file);
            Assert.That(result, Is.EqualTo(container));
        }

        [Test]
        public void GetPackFileContainer_UnknownFile_ReturnsNull()
        {
            var pfs = CreateServiceWithCaPack();
            var unknownFile = new PackFile("unknown.txt", new MemorySource([1]));

            var result = pfs.GetPackFileContainer(unknownFile);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SaveFile_UpdatesFileData()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = CreateServiceWithCaPack(eventHub);
            var container = new PackFileContainer("Custom") { SystemFilePath = "path" };
            container.AddOrUpdateFile("file.txt", new PackFile("file.txt", new MemorySource([1, 2, 3])));
            pfs.AddContainer(container, true);

            var file = container.FindFile("file.txt")!;
            pfs.SaveFile(file, [4, 5, 6]);

            Assert.That(file.DataSource.ReadData(), Is.EqualTo(new byte[] { 4, 5, 6 }));
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileContainerFilesUpdatedEvent>()), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(It.IsAny<PackFileSavedEvent>()), Times.Once);
        }
    }
}
