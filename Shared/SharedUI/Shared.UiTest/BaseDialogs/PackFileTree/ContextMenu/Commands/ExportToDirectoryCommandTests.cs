using System.IO;
using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ExportToDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new ExportToDirectoryCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new ExportToDirectoryCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_IgnoredUntilFilesystemPassTwo()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog()).Returns(new SystemBrowseFolderDialogResult(false, null));
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ExportToDirectoryCommand(CreatePackFileService(owner).Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            dialogs.Verify(x => x.ShowDialogBox(It.Is<string>(s => s.Contains("exported")), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ComputeRelativePath_RepeatedSegmentDoesNotCorruptPath()
        {
            // Root dir is "data", file fullPath is "data\data\unit.mesh" — old Replace would strip both occurrences.
            var result = ExportToDirectoryCommand.ComputeRelativePath("data\\data\\unit.mesh", "data");
            Assert.That(result, Is.EqualTo("\\data\\unit.mesh"));
        }

        [Test]
        public void ComputeRelativePath_NullRootReturnsFullPath()
        {
            var result = ExportToDirectoryCommand.ComputeRelativePath("sub\\file.mesh", null);
            Assert.That(result, Is.EqualTo("\\sub\\file.mesh"));
        }

        [Test]
        public void Execute_ExportMultipleFilesFromRoot_ExportsSuccessfully()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            
            // Create directory structure: root -> [dir -> file1, file2]
            var dir = new TreeNode("models", NodeType.Directory, root);
            
            // Create real PackFile objects with MemorySource
            var packFile1 = new PackFile("mesh1.mesh", new MemorySource(new byte[] { 0x01, 0x02 }));
            var packFile2 = new PackFile("mesh2.mesh", new MemorySource(new byte[] { 0x03, 0x04 }));
            
            var file1 = new TreeNode("mesh1.mesh", NodeType.File, dir);
            var file2 = new TreeNode("mesh2.mesh", NodeType.File, dir);
            
            dir.AddChild(file1);
            dir.AddChild(file2);
            root.AddChild(dir);
            
            var outputDir = "C:\\export";
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(true, outputDir));
            var packFileService = CreatePackFileService(owner);
            packFileService.Setup(x => x.FindFile("models\\mesh1.mesh", owner)).Returns(packFile1);
            packFileService.Setup(x => x.FindFile("models\\mesh2.mesh", owner)).Returns(packFile2);
            
            var fileSystem = new Mock<IFileSystemAccess>();
            // Mock PathGetDirectoryName to extract directory from path
            fileSystem.Setup(x => x.PathGetDirectoryName(It.IsAny<string>()))
                .Returns<string>(p => {
                    if (string.IsNullOrEmpty(p)) return null;
                    var lastSlash = p.LastIndexOf('\\');
                    return lastSlash > 0 ? p.Substring(0, lastSlash) : null;
                });
            fileSystem.Setup(x => x.PathGetFileName(It.IsAny<string>()))
                .Returns<string>(p => {
                    if (string.IsNullOrEmpty(p)) return p;
                    var lastSlash = p.LastIndexOf('\\');
                    return lastSlash >= 0 ? p.Substring(lastSlash + 1) : p;
                });
            
            var command = new ExportToDirectoryCommand(packFileService.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            // Verify files were written
            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(2));
            dialogs.Verify(x => x.ShowDialogBox("2 files exported!", "Export"), Times.Once);
        }
    }
}
