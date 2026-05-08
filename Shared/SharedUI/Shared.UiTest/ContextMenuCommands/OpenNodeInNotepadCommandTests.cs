using System.Threading;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class OpenNodeInNotepadCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new OpenNodeInNotepadCommand(new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(file), Is.True);
            Assert.That(command.IsEnabled(file), Is.True);

            // TODO: Execute may start external process and write temp files depending on local tool installation; skip in first pass.
            Assert.Ignore("TODO: Execute may start external process and write temp files depending on local tool installation; skip in first pass.");
        }
    }
}
