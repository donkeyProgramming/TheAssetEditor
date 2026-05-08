using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class CopyNodePathCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetFullPath(file.Item!, null)).Returns("folder\\file.txt");

            var command = new CopyNodePathCommand(service.Object);

            Assert.That(command.ShouldAdd(file), Is.True);
            Assert.That(command.IsEnabled(file), Is.True);

            command.Execute(file);

            Assert.That(System.Windows.Clipboard.GetText(), Is.EqualTo("folder\\file.txt"));
        }
    }
}
