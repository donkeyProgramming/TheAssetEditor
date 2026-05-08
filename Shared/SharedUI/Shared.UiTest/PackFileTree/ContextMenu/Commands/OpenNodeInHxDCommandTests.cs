using System.Threading;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenNodeInHxDCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFileNode()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new OpenNodeInHxDCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(file), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new OpenNodeInHxDCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(file), Is.True);
        }

        [Test]
        public void Execute_IgnoredUntilExternalProcessPassTwo()
        {
            // TODO: Execute may start external process and write temp files depending on local tool installation; skip in first pass.
            Assert.Ignore("TODO: Execute may start external process and write temp files depending on local tool installation; skip in first pass.");
        }
    }
}
