using System.Threading;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ExportToDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ExportToDirectoryCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ExportToDirectoryCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_IgnoredUntilFilesystemPassTwo()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog()).Returns(new SystemBrowseFolderDialogResult(false, null));
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ExportToDirectoryCommand(dialogs.Object, fileSystem.Object);

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
    }
}
