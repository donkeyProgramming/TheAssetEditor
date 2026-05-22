using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class DuplicateFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFileNode()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["animation\\meta\\testfile.anm"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "animation\\meta\\testfile.anm");

            var command = new DuplicateFileCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["animation\\meta\\testfile.anm"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "animation\\meta\\testfile.anm");

            var command = new DuplicateFileCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(node), Is.True);
        }

        [Test]
        public void Execute_DuplicatesFileIntoEditablePack()
        {
            // Arrange
            var sourceContainer = AddPackFiles(false, "SourcePack", "c:\\source.pack", ["animation\\meta\\testfile.anm"]);
            var outputContainer = AddPackFiles(false, "OutputPack", "c:\\output.pack", []);
            _packFileService.SetEditablePack(outputContainer);

            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, sourceContainer, "animation\\meta\\testfile.anm");

            // Act
            var command = new DuplicateFileCommand(_packFileService, new Mock<IStandardDialogs>().Object);
            command.Configure(node);

            command.Execute();

            // Assert
            var foundFile = outputContainer.FindFile("animation\\meta\\testfile_copy.anm");
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        [TestCase("testfile", "testfile_copy")]
        [TestCase("testfile.anm", "testfile_copy.anm")]
        [TestCase("testfile.anm.meta", "testfile_copy.anm.meta")]
        public void DuplicateFileCommand(string fileName, string result)
        {
            // Arrange
            var sourceContainer = AddPackFiles(false, "SourcePack", "c:\\source.pack", ["animation\\meta\\" + fileName]);
            var outputContainer = AddPackFiles(false, "OutputPack", "c:\\output.pack", []);
            _packFileService.SetEditablePack(outputContainer);

            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, sourceContainer, "animation\\meta\\" + fileName);

            // Act
            var command = new DuplicateFileCommand(_packFileService, new Mock<IStandardDialogs>().Object);
            command.Configure(node);

            command.Execute();

            // Assert
            var foundFile = outputContainer.FindFile("animation\\meta\\" + result);
            Assert.That(foundFile, Is.Not.Null);
        }
    }
}
