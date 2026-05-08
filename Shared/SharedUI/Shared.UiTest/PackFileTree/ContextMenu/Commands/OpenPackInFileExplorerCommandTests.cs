using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenPackInFileExplorerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new OpenPackInFileExplorerCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new OpenPackInFileExplorerCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_IgnoredUntilExternalProcessPassTwo()
        {
            // TODO: Execute launches explorer.exe (external process); skip in first pass.
            Assert.Ignore("TODO: Execute launches explorer.exe (external process); skip in first pass.");
        }

        [Test]
        public void Execute_NullSystemFilePath_ShowsError()
        {
            var owner = CreateContainer(systemFilePath: "");
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var dialogs = new Mock<IStandardDialogs>();
            var command = new OpenPackInFileExplorerCommand(new Mock<IPackFileService>().Object, dialogs.Object, new Mock<IFileSystemAccess>().Object);

            command.Execute(root);

            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
