using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ExportToDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new ExportToDirectoryCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new ExportToDirectoryCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_DialogCancelled_DoesNotExport()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog()).Returns(new SystemBrowseFolderDialogResult(false, null));
            var fileSystem = new Mock<IFileSystemAccess>();

            // Act
            var command = new ExportToDirectoryCommand(_packFileService, dialogs.Object, fileSystem.Object);
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void ComputeRelativePath_RepeatedSegmentDoesNotCorruptPath()
        {
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
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["models\\mesh1.mesh", "models\\mesh2.mesh"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var outputDir = "C:\\export";
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(true, outputDir));

            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.PathGetDirectoryName(It.IsAny<string>()))
                .Returns<string>(p =>
                {
                    if (string.IsNullOrEmpty(p)) return null;
                    var lastSlash = p.LastIndexOf('\\');
                    return lastSlash > 0 ? p.Substring(0, lastSlash) : null;
                });
            fileSystem.Setup(x => x.PathGetFileName(It.IsAny<string>()))
                .Returns<string>(p =>
                {
                    if (string.IsNullOrEmpty(p)) return p;
                    var lastSlash = p.LastIndexOf('\\');
                    return lastSlash >= 0 ? p.Substring(lastSlash + 1) : p;
                });

            // Act
            var command = new ExportToDirectoryCommand(_packFileService, dialogs.Object, fileSystem.Object);
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(2));
            dialogs.Verify(x => x.ShowDialogBox("2 files exported!", "Export"), Times.Once);
        }
    }
}
