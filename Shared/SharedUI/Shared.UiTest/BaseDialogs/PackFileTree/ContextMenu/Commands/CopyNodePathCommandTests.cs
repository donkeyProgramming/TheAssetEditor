using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CopyNodePathCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new CopyNodePathCommand(new Mock<IPackFileService>().Object);

            Assert.That(command.ShouldAdd(file), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new CopyNodePathCommand(new Mock<IPackFileService>().Object);

            Assert.That(command.IsEnabled(file), Is.True);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Execute_CopiesNodePathToClipboard()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetFullPath(file.Item!, null)).Returns("folder\\file.txt");

            var command = new CopyNodePathCommand(service.Object);

            command.Execute(file);

            Assert.That(System.Windows.Clipboard.GetText(), Is.EqualTo("folder\\file.txt"));
        }
    }
}
