using Test.TestingUtility.TestUtility;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenNodeInHxDCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFileNode()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new OpenNodeInHxDCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new OpenNodeInHxDCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

            Assert.That(command.IsEnabled(node), Is.True);
        }

        [Test]
        public void Execute_AppMissing_ShowsError()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

            var command = new OpenNodeInHxDCommand(dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(node);

            command.Execute();

            dialogs.Verify(x => x.ShowDialogBox(It.Is<string>(s => s.Contains("does not exist")), It.IsAny<string>()), Times.Once);
            fileSystem.Verify(x => x.ProcessStart(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_AppExists_WritesTempFileAndStartsProcess()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            var command = new OpenNodeInHxDCommand(dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(node);

            command.Execute();

            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.Is<byte[]>(b => b.Length > 0)), Times.Once);
            fileSystem.Verify(x => x.ProcessStart(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
